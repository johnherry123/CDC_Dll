using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Internal
{
    internal sealed class ByteRingBuffer
    {
        private byte[] _buf;
        private int _head;
        private int _tail;
        private int _count;

        public int Count => _count;
        public int Capacity => _buf.Length;

        public ByteRingBuffer(int capacity = 4096)
        {
            if (capacity < 256) capacity = 256;
            _buf = new byte[capacity];
        }

        public void Clear()
        {
            _head = _tail = _count = 0;
        }

        private void EnsureCapacity(int additional)
        {
            int need = _count + additional;
            if (need <= _buf.Length) return;

            int newCap = _buf.Length;
            while (newCap < need) newCap *= 2;

            var nb = new byte[newCap];
            CopyOut(0, _count, nb.AsSpan(0, _count));

            _buf = nb;
            _head = 0;
            _tail = _count;
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;
            EnsureCapacity(data.Length);

            int first = Math.Min(data.Length, _buf.Length - _tail);
            data.Slice(0, first).CopyTo(_buf.AsSpan(_tail, first));
            int rem = data.Length - first;
            if (rem > 0)
                data.Slice(first, rem).CopyTo(_buf.AsSpan(0, rem));

            _tail = (_tail + data.Length) % _buf.Length;
            _count += data.Length;
        }

        public bool TryPeek(int offset, out byte value)
        {
            value = 0;
            if (offset < 0 || offset >= _count) return false;
            int idx = (_head + offset) % _buf.Length;
            value = _buf[idx];
            return true;
        }

        public void CopyOut(int offset, int length, Span<byte> dst)
        {
            if (length <= 0) return;
            if (offset < 0 || offset + length > _count) throw new ArgumentOutOfRangeException();

            int start = (_head + offset) % _buf.Length;
            int first = Math.Min(length, _buf.Length - start);
            _buf.AsSpan(start, first).CopyTo(dst.Slice(0, first));
            int rem = length - first;
            if (rem > 0)
                _buf.AsSpan(0, rem).CopyTo(dst.Slice(first, rem));
        }

        public void Skip(int length)
        {
            if (length <= 0) return;
            if (length > _count) length = _count;
            _head = (_head + length) % _buf.Length;
            _count -= length;
            if (_count == 0) _head = _tail = 0;
        }
    }
}
