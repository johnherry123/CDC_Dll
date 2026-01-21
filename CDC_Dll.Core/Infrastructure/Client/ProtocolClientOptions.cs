namespace CDC_Dll.Core.Infrastructure.Client;

public class ProtocolClientOptions
{
    public bool EnableAlive{get;init;} = true;
    public TimeSpan AliveInterval{get; init;} = TimeSpan.FromMilliseconds(300);
    public TimeSpan RxWarnAfter{get; init;} = TimeSpan.FromSeconds(2);
    public TimeSpan RxFaultAfter{get; init;} = TimeSpan.FromSeconds(5);
    public TimeSpan DefaultCommandTimeout{get;init;} = TimeSpan.FromMilliseconds(1000);
    public int TelemetryQueueCapacity {get; init;} = 200;
    public int TxQueueCapacity{get; init;} = 500;
}