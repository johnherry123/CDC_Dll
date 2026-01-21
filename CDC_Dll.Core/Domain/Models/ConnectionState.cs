namespace CDC_Dll.Core.Domain.Models;

public enum ConnectionState : byte
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,   
    Faulted = 4

}