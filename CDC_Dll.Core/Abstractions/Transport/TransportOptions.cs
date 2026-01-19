namespace CDC_Dll.Core.Abstractions.Transport;

public sealed class TransportOptions
{
    public string PortName { get; init; } = "COM1";
    public int BaudRate { get; init; } = 115200;

    public bool DtrEnable { get; init; } = true;
    public bool RtsEnable { get; init; } = true;

    public int ReadBufferSize { get; init; } = 8192;
    public int WriteBufferSize { get; init; } = 8192;

    public TimeSpan WriteTimeout { get; init; } = TimeSpan.FromMilliseconds(200);
}