namespace CDC_Dll.Core.Abstractions.Protocol;
public readonly record struct FrameHeader
(
    byte Version,
    MsgType Type,
    ushort MsgId,
    ushort Seq,
    ushort PayloadLength
);