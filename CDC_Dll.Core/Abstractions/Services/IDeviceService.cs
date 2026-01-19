
using CDC.Dll.Core.Domain.Contracts;
using DeviceService.Core.Domain.Contracts;
using DeviceService.Core.Domain.Errors;
using DeviceService.Core.Domain.Models;


namespace DeviceService.Core.Services;
public interface IDeviceService
{
    ConnectionState State { get; }
    event Action<ConnectionState>? StateChanged;
    event Action<Telemetry> TelemetryUpdated;
    Task<Result> ConnectAsync(CancellationToken cancellationToken); 
    Task<Result> DisconnectAsync(CancellationToken cancellationToken);  
    Task<Result<DeviceInfo>> GetDeviceInfoAsync(CancellationToken cancellationToken);
    Task<Result<DeviceStatus>> GetStatusAsync(CancellationToken cancellationToken);
    Task<Result<RegisterReadResponse>> ReadRegistersAsync(RegisterReadRequest request, CancellationToken cancellationToken);
    Task<Result> WriteRegistersAsync(RegisterWriteRequest request, CancellationToken cancellationToken);    
    Task<Result> WriteRegistersAsync(RegisRegisterWriteMultipleRequest req, CancellationToken ct);
    Task<Result> StartAsync(CancellationToken cancellationToken);
    Task<Result> StopAsync(CancellationToken cancellationToken);
    Task<Result> SetTelemetryRateAsync(ushort hz, CancellationToken cancellationToken);

}