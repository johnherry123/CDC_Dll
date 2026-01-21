using CDC_Dll.Core.Domain.Contracts;
using CDC_Dll.Core.Abstractions.Diagnostics;

using CDC_Dll.Core.Abstractions.Protocol;
using CDC_Dll.Core.Application.Commands;
using CDC_Dll.Core.Application.Errors;
using CDC_Dll.Core.Application.Telemetries;
using CDC_Dll.Core.Domain.Contracts;
using CDC_Dll.Core.Domain.Errors;
using CDC_Dll.Core.Domain.Models;
using CDC_Dll.Core.Domain.Protocol;
using CDC_Dll.Core.Infrastructure.Exceptions;
using CDC_Dll.Core.Services;
using CDC_DLL.Core.Abstractions.Protocol;

namespace CDC_Dll.Core.Application.Services;

public sealed class DeviceServiceCore : IDeviceService
{
    private readonly IProtocolClient _Client;
    private readonly IErrorBus _errorBus;
    private readonly TelemetryCache _telemetryCache = new();
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public event Action<ConnectionState>? StateChanged;
    public event Action<Telemetry>? TelemetryUpdated;
    public DeviceServiceCore(IProtocolClient client, IErrorBus errorBus)
    {
        _Client = client;
        _errorBus = errorBus;
        _Client.TelemetryReceived += OnTelemetryFrame;
        _Client.ErrorRaised += err => _errorBus.Publish(err);
    }
    private void SetState(ConnectionState s)
    {
        if (State == s) return;
        State = s;
        StateChanged?.Invoke(s);
    }
    private void OnTelemetryFrame(Frame frame)
    {
        if (frame.Header.Type != MsgType.Telemetry) return;
        try
        {
            var t = TelemetryMapper.Map(frame);
            _telemetryCache.update(t);
            TelemetryUpdated?.Invoke(t);

        }
        catch (Exception ex)
        {
            var ctx = ResultGuard.ctx("TelemetryMap", component: nameof(DeviceService));
            var err = ErrorMapper.FromException(ex, ctx, "Invalid telemetry data. Please check your device/connection.");
            _errorBus.Publish(err);
        }
    }
    public async Task<Result> ConnectAsync(CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("Connect", component: nameof(DeviceService));
        try
        {
            SetState(ConnectionState.Connecting);
            await _Client.StartAsync(ct);
            SetState(ConnectionState.Connected);
            return Result.OK();
        }
        catch (Exception ex)
        {
            SetState(ConnectionState.Faulted);
            var r = ResultGuard.Fail(ex, ctx, "The device could not be connected. Please check the cable/COM port and try again.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result> DisconnectAsync(CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("Disconnect", component: nameof(DeviceInfo));
        try
        {
            await _Client.StopAsync(ct);
            SetState(ConnectionState.Disconnected);
            return Result.OK();
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail(ex, ctx, "Unable to disconnect. Please try again.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result<DeviceInfo>> GetDeviceInfoAsync(CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("GetDeviceInfor", component: nameof(DeviceService));
        try
        {
            var payload = CommandPayloadFactory.Build(CommandId.GetDeviceInfo);
            var cmdFrame = BuildCommandFrame(payload);
            var respFrame = await _Client.SendCommandAsync(cmdFrame, timeout: TimeSpan.FromMilliseconds(800), ct);
            var parsed = ResponseParser.Parse(respFrame.Payload);
            if (parsed.Status != ResponeStatus.Ok)
            {
                throw new DeviceRejectException(ErrorCode.DeviceRejected, parsed.DeviceErrorCode, $"Device rejected {parsed.CommandId} status={parsed.Status}");
            }
            var info = new DeviceInfo
            {
                Vendor = "Unknown",
                Product = "Unknown",
                FirmwareVersion = "0.0",
                SerialNumber = ""
            };
            return Result<DeviceInfo>.OK(info);

        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail<DeviceInfo>(ex, ctx, "Unable to read device information.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result<DeviceStatus>> GetStatusAsync(CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("GetStatus", component: nameof(DeviceService));
        try
        {
            var payload = CommandPayloadFactory.Build(CommandId.GetStatus);
            var cmdFrame = BuildCommandFrame(payload);
            var respFrame = await _Client.SendCommandAsync(cmdFrame, timeout: TimeSpan.FromMilliseconds(600), ct);
            var parsed = ResponseParser.Parse(respFrame.Payload);
            if (parsed.Status != ResponeStatus.Ok)
                throw new DeviceRejectException(ErrorCode.DeviceError, parsed.DeviceErrorCode, $"Device error {parsed.CommandId} status={parsed.Status}");
            var st = new DeviceStatus
            {
                isRunning = false,
                Mode = 0,
                ErrorCode = parsed.DeviceErrorCode,
                LastUpdatedUTC = DateTime.UtcNow
            };
            return Result<DeviceStatus>.OK(st);
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail<DeviceStatus>(ex, ctx, "The device status cannot be read.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result> StartAsync(CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("Start", component: nameof(DeviceService));
        try
        {
            var payload = CommandPayloadFactory.Build(CommandId.Start);
            var cmdFrame = BuildCommandFrame(payload);
            var respFrame = await _Client.SendCommandAsync(cmdFrame, TimeSpan.FromMilliseconds(800), ct);
            var parsed = ResponseParser.Parse(respFrame.Payload);
            if (parsed.Status != ResponeStatus.Ok)
                throw new DeviceRejectException(ErrorCode.DeviceRejected, parsed.DeviceErrorCode, "The device rejected the Start command.");
            return Result.OK();
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail(ex, ctx, "The device will not start. Please check the device status.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result> StopAsync(CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("Stop", component: nameof(DeviceService));
        try
        {
            var payload = CommandPayloadFactory.Build(CommandId.Stop);
            var cmdFrame = BuildCommandFrame(payload);
            var respFrame = await _Client.SendCommandAsync(cmdFrame, TimeSpan.FromMilliseconds(800), ct);
            var parsed = ResponseParser.Parse(respFrame.Payload);
            if (parsed.Status != ResponeStatus.Ok)
                throw new DeviceRejectException(ErrorCode.DeviceRejected, parsed.DeviceErrorCode, "The device rejected the Stop command.");
            return Result.OK();
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail(ex, ctx, "Cannot be stopped. Please try again.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result> SetTelemetryRateAsync(ushort hz, CancellationToken ct)
    {
        var ctx = ResultGuard.ctx("SetTelemetryRate", detail: $"Hz = {hz}", component: nameof(DeviceService));
        try
        {
            var payload = CommandPayloadFactory.BuildSetTelemetryRate(hz);
            var cmdFrame = BuildCommandFrame(payload);
            var respFrame = await _Client.SendCommandAsync(cmdFrame, TimeSpan.FromMilliseconds(800), ct);
            var parsed = ResponseParser.Parse(respFrame.Payload);
            if (parsed.Status != ResponeStatus.Ok)
                throw new DeviceRejectException(ErrorCode.DeviceRejected, parsed.DeviceErrorCode, "The device refused to change the telemetry speed.");
            return Result.OK();
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail(ex, ctx, "The telemetry speed cannot be changed.");
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    private static Frame BuildCommandFrame(ReadOnlyMemory<byte> commandPayload)
    {
        var header = new FrameHeader(
            Version: ProtocolVersion.Current,
            Type: MsgType.Command,
            MsgId: 0,
            Seq: 0,
            PayloadLength: (ushort)commandPayload.Length
        );
        return new Frame(header, commandPayload);
    }
    public async Task<Result<RegisterReadResponse>> ReadRegistersAsync(RegisterReadRequest req, CancellationToken ct)
    {
        var ctx = ResultGuard.ctx(
            operation: "ReadRegisters",
            detail: $"Type={req.Type}, Start={req.StartAddress}, Count={req.Count}, UnitId={req.UnitId}",
            component: nameof(DeviceService)
        );

        try
        {
            await Task.CompletedTask;
            var res = new RegisterReadResponse
            {
                UnitId = req.UnitId,
                Type = req.Type,
                StartAddress = req.StartAddress,
                Value = Array.Empty<ushort>()
            };

            return Result<RegisterReadResponse>.OK(res);
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail<RegisterReadResponse>(
                ex, ctx,
                "Unable to read the Register. Please check your connection/device."
            );
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result> WriteRegistersAsync(RegisterWriteRequest req, CancellationToken ct)
    {
        var ctx = ResultGuard.ctx(
            operation: "WriteRegister",
            detail: $"Type={req.Type}, Addr={req.Address}, Value={req.Value}, UnitId={req.UnitId}",
            component: nameof(DeviceService)
        );
        try
        {
            await Task.CompletedTask;

            return Result.OK();
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail(
                ex, ctx,
                "Register failed. Please try again or check your device."
            );
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
    public async Task<Result> WriteRegistersAsync(RegisRegisterWriteMultipleRequest req, CancellationToken ct)
    {
        var count = req.Values?.Length ?? 0;

        var ctx = ResultGuard.ctx(
            operation: "WriteRegisters",
            detail: $"Type={req.Type}, Start={req.StartAddress}, Count={count}, UnitId={req.UnitId}",
            component: nameof(DeviceService)
        );
        try
        {
            if (req.Values is null || req.Values.Length == 0)
                return Result.Fail(new ErrorInfo
                {
                    Code = ErrorCode.ProtocolPayloadInvalid,
                    Category = ErrorCategory.Protocol,
                    Severity = ErrorServerity.Warning,
                    UserMessage = "The list of values ​​is empty.",
                    TechnicalMessage = "RegisterWriteMultipleRequest.Values is null/empty.",
                    Context = ctx
                });

            await Task.CompletedTask;
            return Result.OK();
        }
        catch (Exception ex)
        {
            var r = ResultGuard.Fail(
                ex, ctx,
                "Unable to write multiple registers. Please try again or check your device."
            );
            _errorBus.Publish(r.Error!);
            return r;
        }
    }
}