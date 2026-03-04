using McuCdc.Modbus.Client;
using RJCP.IO.Ports;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Transport
{
    internal sealed class SerialPortStreamTransport : ISerialTransport
    {
        private readonly ClientOptions _opt;

        // _gate chỉ dùng để bảo vệ open/close + swap _port
        private readonly object _gate = new();

        private SerialPortStream? _port;

        // đóng cưỡng bức để cắt I/O đang treo
        private int _closing; // 0 = open, 1 = closing/closed

        public bool IsOpen
        {
            get
            {
                var p = Volatile.Read(ref _port);
                return p?.IsOpen == true && Volatile.Read(ref _closing) == 0;
            }
        }

        public DateTime LastReadUtc { get; private set; } = DateTime.MinValue;
        public DateTime LastWriteUtc { get; private set; } = DateTime.MinValue;

        public SerialPortStreamTransport(ClientOptions opt)
        {
            _opt = opt ?? throw new ArgumentNullException(nameof(opt));
        }

        public ValueTask OpenAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            lock (_gate)
            {
                if (_port?.IsOpen == true && _closing == 0)
                    return ValueTask.CompletedTask;

               
                Volatile.Write(ref _closing, 0);

             
                try { _port?.Dispose(); } catch { }
                _port = null;

                var p = new SerialPortStream(_opt.PortName, _opt.BaudRate, _opt.DataBits, _opt.Parity, _opt.StopBits)
                {
                    Handshake = _opt.Handshake,
                    ReadTimeout = _opt.ReadTimeoutMs,  
                    WriteTimeout = _opt.WriteTimeoutMs,
                    DtrEnable = _opt.DtrEnable,
                    RtsEnable = _opt.RtsEnable
                };

              
                p.Open();

                _port = p;

                LastReadUtc = DateTime.UtcNow;
                LastWriteUtc = DateTime.UtcNow;

                return ValueTask.CompletedTask;
            }
        }

   
        public ValueTask CloseAsync()
        {
            ForceCloseInternal();
            return ValueTask.CompletedTask;
        }

        public void ForceClose()
        {
            ForceCloseInternal();
        }

        private void ForceCloseInternal()
        {
           
            Interlocked.Exchange(ref _closing, 1);

            SerialPortStream? p;
            lock (_gate)
            {
                p = _port;
                _port = null;
            }

            if (p is null) return;

            try
            {
                try { if (p.IsOpen) p.Close(); } catch { }
            }
            finally
            {
                try { p.Dispose(); } catch { }
            }
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
        {
            if (Volatile.Read(ref _closing) != 0)
                throw new OperationCanceledException("Transport is closing.", ct);

            var p = Volatile.Read(ref _port) ?? throw new InvalidOperationException("Port not open");

            try
            {
             
                int n = await p.ReadAsync(buffer, ct).ConfigureAwait(false);
                if (n > 0) LastReadUtc = DateTime.UtcNow;
                return n;
            }
            catch (TimeoutException)
            {
               
                return 0;
            }
            catch (ObjectDisposedException ex)
            {
         
                throw new IOException("Port was closed while reading.", ex);
            }
            catch (InvalidOperationException ex)
            {
          
                throw new IOException("Port is not available for reading.", ex);
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct)
        {
            if (Volatile.Read(ref _closing) != 0)
                throw new OperationCanceledException("Transport is closing.", ct);

            var p = Volatile.Read(ref _port) ?? throw new InvalidOperationException("Port not open");

            try
            {
                await p.WriteAsync(buffer, ct).ConfigureAwait(false);
                LastWriteUtc = DateTime.UtcNow;
            }
            catch (ObjectDisposedException ex)
            {
                throw new IOException("Port was closed while writing.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new IOException("Port is not available for writing.", ex);
            }
        }

        public ValueTask DisposeAsync()
        {
            ForceCloseInternal();
            return ValueTask.CompletedTask;
        }
    }
}
