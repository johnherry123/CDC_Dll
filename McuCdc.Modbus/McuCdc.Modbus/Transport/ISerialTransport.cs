using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Transport
{
    internal interface ISerialTransport : IAsyncDisposable
    {
        bool IsOpen { get; }

        DateTime LastReadUtc { get; }
        DateTime LastWriteUtc { get; }

        ValueTask OpenAsync(CancellationToken ct);
        ValueTask CloseAsync();

        ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct);
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct);
    }
}
