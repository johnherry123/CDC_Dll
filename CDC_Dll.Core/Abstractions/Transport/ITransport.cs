namespace CDC_Dll.Core.Abstractions.Transport;

public interface ITransport: IAsyncDisposable
{
    string Name { get; }
    bool IsConnected { get; }
    ValueTask OpenAsync(CancellationToken cancellationToken = default); 
    ValueTask CloseAsync(CancellationToken cancellationToken = default);
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);   
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

}