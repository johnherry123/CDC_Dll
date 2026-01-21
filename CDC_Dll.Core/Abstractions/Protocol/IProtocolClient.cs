using CDC_Dll.Core.Domain.Errors;
using CDC_Dll.Core.Domain.Protocol;

namespace CDC_Dll.Core.Abstractions.Protocol;

public interface IProtocolClient : IAsyncDisposable
{
    bool isRunning { get; }
    ConnectionHealthSnapshot Health{ get; }
    event Action<Frame> TelemetryReceived;
    event Action<ErrorInfo> ErrorRaised;
    ValueTask StartAsync(CancellationToken cancellationToken );
    ValueTask StopAsync(CancellationToken cancellationToken );
    Task<Frame> SendCommandAsync(Frame commandFrame, TimeSpan timeout,CancellationToken cancellationToken );
    LinkMetricsSnapshot GetLinkMetricsSnapshot();

}