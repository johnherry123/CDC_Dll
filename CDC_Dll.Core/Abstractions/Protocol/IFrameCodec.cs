using CDC_Dll.Core.Domain.Protocol;

namespace CDC_Dll.Core.Abstractions.Protocol;

public interface IFrameCodec
{
    void Reset();
    IEnumerable<Frame> Feed(ReadOnlySpan<byte> data);
    byte[] Encode(Frame frame);
}