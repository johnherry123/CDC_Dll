namespace CDC_Dll.Core.Domain.Errors;
public enum ErrorCode : ushort
{
    None = 0,

    // Transport Errors 1001 - 1003
    TransportDisconnected = 1001,
    TransportReadFailed = 1002,
    TransportWriteFailed = 1003,    
    // Protocol Errors 2001 - 2003
    ProtocolCRCFailed = 2001,
    ProtocolFrameInvalid = 2002,    
    ProtocolPayloadInvalid = 2003,
    // Timeout Errors 3001 - 3002
    CommandTimeout = 3001,
    AliveTimeout = 3002,
    // Device Errors 4001 - 4003
    DeviceRejected = 4001,
    DeviceBusy = 4002,
    DeviceError = 4003,
    //Internal Errors 9001 
    InternalError = 9001

}