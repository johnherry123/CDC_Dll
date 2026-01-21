namespace CDC_Dll.Core.Abstractions.Protocol;
public static class ProtocolConstants
{
    public const byte Sof1 = 0xA5;
    public const byte Sof2 = 0x5A;
    //Ver(1) + Type(1) + MsgID(2) +Seq(2) + Len(2) 
    public const int HeaderLength = 8;
    public const int CrcLength = 2; 
    public const int MaxPayloadLength = 4096;   
}