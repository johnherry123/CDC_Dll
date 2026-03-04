using McuCdc.Modbus.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Spooling
{
    internal sealed class FrameSpoolFile : IDisposable
    {
        private readonly SpoolOptions _opt;

        private FileStream? _ws;
        private string? _openPath;
        private long _writeBytes;
        private int _flushCounter;

        private FileStream? _rs;
        private string? _readPath;

        public FrameSpoolFile(SpoolOptions opt)
        {
            _opt = opt ?? throw new ArgumentNullException(nameof(opt));
            Directory.CreateDirectory(_opt.DirectoryPath);
        }

        private void EnsureWriter()
        {
            if (_ws is not null) return;

            long id = DateTime.UtcNow.Ticks % 1_000_000;
            _openPath = Path.Combine(_opt.DirectoryPath, $"seg_{id:000000}.open");

            _ws = new FileStream(
                _openPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.SequentialScan);

            _writeBytes = 0;
            _flushCounter = 0;
        }

        private void Roll()
        {
            if (_ws is null || _openPath is null) return;

            _ws.Flush(true);
            _ws.Dispose();

            var finalPath = Path.ChangeExtension(_openPath, ".bin");
            try
            {
                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(_openPath, finalPath);
            }
            catch { }

            _ws = null;
            _openPath = null;
            _writeBytes = 0;
            _flushCounter = 0;
        }

        public void Enqueue(PooledBuffer frame)
        {
            try
            {
                EnsureWriter();
                ushort len = checked((ushort)frame.Length);

                Span<byte> hdr = stackalloc byte[2];
                hdr[0] = (byte)(len & 0xFF);
                hdr[1] = (byte)(len >> 8);

                _ws!.Write(hdr);
                _ws.Write(frame.Span);

                _writeBytes += 2 + len;
                _flushCounter++;

                if (_flushCounter >= Math.Max(1, _opt.FlushEveryNFrames))
                {
                    _ws.Flush(false);
                    _flushCounter = 0;
                }

                if (_writeBytes >= _opt.MaxSegmentBytes)
                    Roll();
            }
            finally
            {
                frame.Dispose();
            }
        }

        private string? PickOldestBin()
        {
            var f = Directory.EnumerateFiles(_opt.DirectoryPath, "seg_*.bin")
                .OrderBy(x => x, StringComparer.Ordinal)
                .FirstOrDefault();
            return f;
        }

        private void EnsureReader()
        {
            if (_rs is not null) return;
            var path = PickOldestBin();
            if (path is null) return;

            _rs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 64 * 1024,
                options: FileOptions.SequentialScan);

            _readPath = path;
        }

        private void CloseReadAndDelete()
        {
            try { _rs?.Dispose(); } catch { }
            _rs = null;

            if (_readPath is not null)
            {
                try { File.Delete(_readPath); } catch { }
                _readPath = null;
            }
        }

        public bool TryDequeue(out PooledBuffer frame)
        {
            frame = null!;
            EnsureReader();
            if (_rs is null) return false;

            Span<byte> hdr = stackalloc byte[2];
            int r0 = _rs.Read(hdr);
            if (r0 == 0) { CloseReadAndDelete(); return false; }
            if (r0 < 2) { CloseReadAndDelete(); return false; }

            ushort len = (ushort)(hdr[0] | (hdr[1] << 8));
            if (len < 5 || len > 256) { CloseReadAndDelete(); return false; }

            var pb = new PooledBuffer(len);
            int need = len, off = 0;
            while (need > 0)
            {
                int n = _rs.Read(pb.Span.Slice(off, need));
                if (n <= 0) { pb.Dispose(); CloseReadAndDelete(); return false; }
                off += n;
                need -= n;
            }

            frame = pb;
            return true;
        }

        public void Dispose()
        {
            try { Roll(); } catch { }
            try { _rs?.Dispose(); } catch { }
            try { _ws?.Dispose(); } catch { }
        }
    }
}
