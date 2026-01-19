using DeviceService.Core.Abstractions.Protocol;

namespace DeviceService.Core.Domain.Protocol;

public sealed class Frame
{
    public FrameHeader Header { get;  }
    public ReadOnlyMemory<byte> Payload { get;  }   
    public Frame(FrameHeader header, ReadOnlyMemory<byte> payload)
    {
        Header = header;
        Payload = payload;
    }
    public override string ToString()
    {
        return  $"V={Header.Version}, Type={Header.Type}, MsgId={Header.MsgId}, Seq={Header.Seq}, Len={Header.PayloadLength}";
    }

}