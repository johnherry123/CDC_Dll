using DeviceService.Core.Domain.Protocol;

namespace DeviceService.Core.Abstractions.Protocol;

public interface IFrameCodec
{
    void Reset();
    IEnumerable<Frame> Feed(ReadOnlySpan<byte> data);
    byte[] Encode(Frame frame);
}