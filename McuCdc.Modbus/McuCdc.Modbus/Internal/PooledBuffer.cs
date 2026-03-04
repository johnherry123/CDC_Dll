using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Internal
{
    public sealed class PooledBuffer : IDisposable
    {
        private byte[]? _array;
        private readonly ArrayPool<byte> _pool;

        public int Length { get; private set; }

        public PooledBuffer(int length) : this(length, ArrayPool<byte>.Shared) { }

        public PooledBuffer(int length, ArrayPool<byte> pool)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _array = _pool.Rent(length);
            Length = length;
        }

        public Span<byte> Span
            => _array is null ? Span<byte>.Empty : _array.AsSpan(0, Length);

        public Memory<byte> Memory
            => _array is null ? Memory<byte>.Empty : _array.AsMemory(0, Length);

        public ReadOnlyMemory<byte> ReadOnlyMemory
            => _array is null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(_array, 0, Length);

        public void Dispose()
        {
            var a = _array;
            _array = null;
            if (a is not null)
                _pool.Return(a);
            Length = 0;
        }
    }
}
