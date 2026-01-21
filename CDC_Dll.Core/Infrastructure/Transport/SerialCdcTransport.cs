


using CDC_Dll.Core.Abstractions.Transport;
using CDC_Dll.Core.Domain.Errors;
using CDC_Dll.Core.Infrastructure.Exceptions;
using RJCP.IO.Ports;

namespace CDC_Dll.Core.Infrastructure.Transport;

public sealed class SerialCdcTransport : ITransport
{
    private readonly SerialPortStream _sp;
    private readonly TransportOptions _opt;

    public string Name => _opt.PortName;
    public bool IsOpen => _sp.IsOpen;
    public bool IsConnected => _sp.IsOpen;
    public SerialCdcTransport(TransportOptions options)
    {
        _opt = options;

        _sp = new SerialPortStream(options.PortName, options.BaudRate)
        {
            DtrEnable = options.DtrEnable,
            RtsEnable = options.RtsEnable,
            ReadTimeout = 0,
            WriteTimeout = (int)options.WriteTimeout.TotalMilliseconds
        };
    }
    public ValueTask OpenAsync(CancellationToken ct)
    {
        try
        {
            if (!_sp.IsOpen) _sp.Open();
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new TransportException(ErrorCode.TransportDisconnected,
                $"Open COM failed ({_opt.PortName}).", ex);
        }
    }
    public ValueTask CloseAsync(CancellationToken ct)
    {
        try
        {
            if (_sp.IsOpen) _sp.Close();
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new TransportException(ErrorCode.TransportDisconnected,
                $"Close COM failed ({_opt.PortName}).", ex);
        }
    }
    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
    {
        try
        {
            return await _sp.ReadAsync(buffer, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new TransportException(ErrorCode.TransportReadFailed,
                $"Read failed ({_opt.PortName}).", ex);
        }
    }
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        try
        {
            await _sp.WriteAsync(data, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new TransportException(ErrorCode.TransportWriteFailed,
                $"Write failed ({_opt.PortName}).", ex);
        }
    }
    public ValueTask DisposeAsync()
    {
        try { _sp.Dispose(); } catch { }
        return ValueTask.CompletedTask;
    }
}
