using McuCdc.Modbus.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Client
{
    public readonly struct FrameLease : IDisposable
    {
        private readonly PooledBuffer? _buf;

        internal FrameLease(PooledBuffer buf) => _buf = buf;

        public ReadOnlyMemory<byte> Memory => _buf?.ReadOnlyMemory ?? ReadOnlyMemory<byte>.Empty;
        public int Length => _buf?.Length ?? 0;

        public void Dispose() => _buf?.Dispose();
    }
}
