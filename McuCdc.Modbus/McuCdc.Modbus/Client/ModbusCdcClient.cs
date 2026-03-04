#nullable enable
using System;
using System.Buffers;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using McuCdc.Modbus.Client;
using RJCP.IO.Ports;

namespace McuCdc.Modbus;
public sealed class ModbusCdcClient : IAsyncDisposable
{

    public enum AddressingMode
    {
        
        AsProvided = 0,
      
        OneBasedDisplay = 1
    }

    public sealed class Options
    {
 
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public bool DtrEnable { get; set; } = false;
        public bool RtsEnable { get; set; } = false;

        public AddressingMode Addressing { get; set; } = AddressingMode.OneBasedDisplay;
        public int ReadChunkBytes { get; set; } = 4096;
        public int RingBufferBytes { get; set; } = 16 * 1024;
        public int MaxQueuedRequests { get; set; } = 256;
        public int RxQueueCapacity { get; set; } = 1024;
        public int TxQueueCapacity { get; set; } = 256;
        public int DefaultTimeoutMs { get; set; } = 800;
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectDelayMs { get; set; } = 300;
        public int WatchdogPeriodMs { get; set; } = 500;
        public int StallReadMs { get; set; } = 2500;
    }
    public readonly struct FrameLease : IDisposable
    {
        private readonly PooledBuffer? _pb;

        internal FrameLease(PooledBuffer pb) => _pb = pb;  

        public ReadOnlyMemory<byte> Memory => _pb?.ReadOnlyMemory ?? ReadOnlyMemory<byte>.Empty;
        public int Length => _pb?.Length ?? 0;

        public void Dispose() => _pb?.Dispose();
    }

    public event Action<Exception, string>? Faulted;

    public event Action<ReadOnlyMemory<byte>>? UnsolicitedFrame;

    public bool IsRunning => _cts != null;

    private readonly Options _opt;

    private SerialPortStream? _port;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    private CancellationTokenSource? _cts;
    private Task? _readTask, _writeTask, _dispatchTask, _requestTask, _watchdogTask;

    private readonly Channel<PooledBuffer> _txQ;
    private readonly Channel<PooledBuffer> _rxQ;
    private readonly Channel<RequestWorkItem> _reqQ;

    private readonly ModbusRtuExtractor _extractor;

    private DateTime _lastReadUtc;

    private Inflight? _inflight; 

    public ModbusCdcClient(Options opt)
    {
        _opt = opt ?? throw new ArgumentNullException(nameof(opt));
        _extractor = new ModbusRtuExtractor(_opt.RingBufferBytes);

        _txQ = Channel.CreateBounded<PooledBuffer>(new BoundedChannelOptions(_opt.TxQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _rxQ = Channel.CreateBounded<PooledBuffer>(new BoundedChannelOptions(_opt.RxQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _reqQ = Channel.CreateBounded<RequestWorkItem>(new BoundedChannelOptions(_opt.MaxQueuedRequests)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public void InstallGlobalExceptionHandlers(bool captureFirstChance = false)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                if (e.ExceptionObject is Exception ex) RaiseFault(ex, "AppDomain.UnhandledException");
                else RaiseFault(new Exception($"UnhandledException: {e.ExceptionObject}"), "AppDomain.UnhandledException");
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try
            {
                RaiseFault(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            }
            catch { }
        };

        if (captureFirstChance)
        {
            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                try { RaiseFault(e.Exception, "AppDomain.FirstChanceException"); } catch { }
            };
        }
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var runCt = _cts.Token;

        await OpenPortAsync(runCt).ConfigureAwait(false);

        _lastReadUtc = DateTime.UtcNow;

        _readTask = Task.Run(() => ReadLoopAsync(runCt), runCt);
        _writeTask = Task.Run(() => WriteLoopAsync(runCt), runCt);
        _dispatchTask = Task.Run(() => DispatchLoopAsync(runCt), runCt);
        _requestTask = Task.Run(() => RequestLoopAsync(runCt), runCt);
        _watchdogTask = Task.Run(() => WatchdogLoopAsync(runCt), runCt);
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts == null) return;

        _cts = null;
        try { cts.Cancel(); } catch { }
        var infl = Interlocked.Exchange(ref _inflight, null);
        infl?.TryFail(new OperationCanceledException("Client stopped"));

        try
        {
            await Task.WhenAll(
                _readTask ?? Task.CompletedTask,
                _writeTask ?? Task.CompletedTask,
                _dispatchTask ?? Task.CompletedTask,
                _requestTask ?? Task.CompletedTask,
                _watchdogTask ?? Task.CompletedTask
            ).ConfigureAwait(false);
        }
        catch { }

        _readTask = _writeTask = _dispatchTask = _requestTask = _watchdogTask = null;

        await ClosePortAsync().ConfigureAwait(false);

        try { cts.Dispose(); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _reconnectLock.Dispose();
    }

    public Task<bool[]> ReadCoilsAsync(byte slave, ushort startCoilDisplay, ushort qty, int timeoutMs = 0, CancellationToken ct = default)
        => ReadBitsAsync(slave, 0x01, NormalizeAddr(startCoilDisplay), qty, timeoutMs, ct);

    public Task<bool[]> ReadDiscreteInputsAsync(byte slave, ushort startInputDisplay, ushort qty, int timeoutMs = 0, CancellationToken ct = default)
        => ReadBitsAsync(slave, 0x02, NormalizeAddr(startInputDisplay), qty, timeoutMs, ct);

    public Task<ushort[]> ReadHoldingRegistersAsync(byte slave, ushort startRegDisplay, ushort qty, int timeoutMs = 0, CancellationToken ct = default)
        => ReadRegistersAsync(slave, 0x03, NormalizeAddr(startRegDisplay), qty, timeoutMs, ct);

    public Task<ushort[]> ReadInputRegistersAsync(byte slave, ushort startRegDisplay, ushort qty, int timeoutMs = 0, CancellationToken ct = default)
        => ReadRegistersAsync(slave, 0x04, NormalizeAddr(startRegDisplay), qty, timeoutMs, ct);

    public Task WriteSingleCoilAsync(byte slave, ushort coilDisplay, bool value, int timeoutMs = 0, CancellationToken ct = default)
        => WriteSingleCoilCoreAsync(slave, NormalizeAddr(coilDisplay), value, timeoutMs, ct);

    public Task WriteSingleRegisterAsync(byte slave, ushort regDisplay, ushort value, int timeoutMs = 0, CancellationToken ct = default)
        => WriteSingleRegisterCoreAsync(slave, NormalizeAddr(regDisplay), value, timeoutMs, ct);

    public Task WriteMultipleCoilsAsync(byte slave, ushort startCoilDisplay, bool[] values, int timeoutMs = 0, CancellationToken ct = default)
        => WriteMultipleCoilsCoreAsync(slave, NormalizeAddr(startCoilDisplay), values, timeoutMs, ct);

    public Task WriteMultipleRegistersAsync(byte slave, ushort startRegDisplay, ushort[] values, int timeoutMs = 0, CancellationToken ct = default)
        => WriteMultipleRegistersCoreAsync(slave, NormalizeAddr(startRegDisplay), values, timeoutMs, ct);

    public async Task<FrameLease> RequestRawAsync(
        ReadOnlyMemory<byte> requestRtu,
        byte slave,
        byte function,
        RawMatchKind matchKind,
        ushort startOrAddr = 0,
        ushort qty = 0,
        ushort echoValue = 0,
        int timeoutMs = 0,
        CancellationToken ct = default)
    {
        if (!IsRunning) throw new InvalidOperationException("Client not started.");
        if (timeoutMs <= 0) timeoutMs = _opt.DefaultTimeoutMs;

        var req = new PooledBuffer(requestRtu.Length);
        requestRtu.Span.CopyTo(req.Span);

        var matcher = InflightMatcher.FromRaw(slave, function, matchKind, startOrAddr, qty, echoValue);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        return new FrameLease(respPb);
    }

    public enum RawMatchKind
    {
        Basic = 0,
        ReadBits = 1,
        ReadRegisters = 2,
        Fc05 = 3,
        Fc06 = 4,
        Fc0F = 5,
        Fc10 = 6
    }
    private ushort NormalizeAddr(ushort uiAddr)
    {
        if (_opt.Addressing == AddressingMode.OneBasedDisplay && uiAddr > 0)
            return (ushort)(uiAddr - 1);
        return uiAddr;
    }

    private void RaiseFault(Exception ex, string where)
    {
        try { Faulted?.Invoke(ex, where); } catch { }
    }

    private bool PortOpen => _port?.IsOpen == true;

    private async Task OpenPortAsync(CancellationToken ct)
    {
        await _reconnectLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (PortOpen) return;

            var p = new SerialPortStream(_opt.PortName, _opt.BaudRate, _opt.DataBits, _opt.Parity, _opt.StopBits)
            {
                Handshake = _opt.Handshake,
                ReadTimeout = 50,
                WriteTimeout = 200,
                DtrEnable = _opt.DtrEnable,
                RtsEnable = _opt.RtsEnable
            };

            p.Open();
            _port = p;
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private Task ClosePortAsync()
    {
        try
        {
            var p = _port;
            _port = null;

            if (p != null)
            {
                try { if (p.IsOpen) p.Close(); } catch { }
                try { p.Dispose(); } catch { }
            }
        }
        catch { }
        return Task.CompletedTask;
    }

    private async Task ReconnectHardAsync(string reason, CancellationToken ct)
    {
        if (!_opt.AutoReconnect) return;

        await _reconnectLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            RaiseFault(new IOException($"ReconnectHard: {reason}"), "reconnect");

            var infl = Interlocked.Exchange(ref _inflight, null);
            infl?.TryFail(new IOException("Reconnected / inflight aborted"));

            await ClosePortAsync().ConfigureAwait(false);
            _extractor.Reset();

            await Task.Delay(_opt.ReconnectDelayMs, ct).ConfigureAwait(false);

            var p = new SerialPortStream(_opt.PortName, _opt.BaudRate, _opt.DataBits, _opt.Parity, _opt.StopBits)
            {
                Handshake = _opt.Handshake,
                ReadTimeout = 50,
                WriteTimeout = 200,
                DtrEnable = _opt.DtrEnable,
                RtsEnable = _opt.RtsEnable
            };
            p.Open();
            _port = p;

            _lastReadUtc = DateTime.UtcNow;
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var rented = ArrayPool<byte>.Shared.Rent(Math.Max(256, _opt.ReadChunkBytes));
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!PortOpen)
                {
                    await Task.Delay(50, ct).ConfigureAwait(false);
                    continue;
                }

                int n = 0;
                try
                {
                    n = await _port!.ReadAsync(rented, 0, rented.Length, ct).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    n = 0;
                }
                catch (Exception ex)
                {
                    RaiseFault(ex, "ReadLoop.Read");
                    if (_opt.AutoReconnect)
                    {
                        await ReconnectHardAsync("read exception", ct).ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }

                if (n <= 0) continue;

                _lastReadUtc = DateTime.UtcNow;

                _extractor.Push(rented.AsSpan(0, n));

                while (_extractor.TryPop(out var frame))
                {
                    if (!_rxQ.Writer.TryWrite(frame))
                        frame.Dispose();
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private async Task WriteLoopAsync(CancellationToken ct)
    {
        var reader = _txQ.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var pb))
            {
                try
                {
                    if (!PortOpen)
                        await ReconnectHardAsync("write while closed", ct).ConfigureAwait(false);

                    await _port!.WriteAsync(pb.Buffer, 0, pb.Length, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    RaiseFault(ex, "WriteLoop.Write");
                    if (_opt.AutoReconnect)
                    {
                        await ReconnectHardAsync("write exception", ct).ConfigureAwait(false);
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    pb.Dispose();
                }
            }
        }
    }

    private async Task DispatchLoopAsync(CancellationToken ct)
    {
        var reader = _rxQ.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var pb))
            {
                bool transferred = false;
                try
                {
                    var view = new ModbusFrameView(pb.ReadOnlyMemory);

                    var infl = Volatile.Read(ref _inflight);
                    if (infl != null && infl.Matcher.IsMatch(view))
                    {
                        if (ReferenceEquals(Interlocked.CompareExchange(ref _inflight, null, infl), infl))
                        {
                            infl.TryComplete(pb); 
                            transferred = true;
                            continue;
                        }
                    }
                    try { UnsolicitedFrame?.Invoke(pb.ReadOnlyMemory); } catch { }
                }
                catch (Exception ex)
                {
                    RaiseFault(ex, "DispatchLoop");
                    var infl = Interlocked.Exchange(ref _inflight, null);
                    infl?.TryFail(ex);
                }
                finally
                {
                    if (!transferred) pb.Dispose();
                }
            }
        }
    }

    private async Task RequestLoopAsync(CancellationToken ct)
    {
        var reader = _reqQ.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var wi))
            {
                if (wi.Ct.IsCancellationRequested)
                {
                    wi.Fail(new OperationCanceledException(wi.Ct));
                    continue;
                }

                var inflight = new Inflight(wi.Matcher, wi.Tcs);
                if (Interlocked.CompareExchange(ref _inflight, inflight, null) != null)
                {
                    wi.Fail(new InvalidOperationException("Invariant violated: inflight already set"));
                    continue;
                }
                await _txQ.Writer.WriteAsync(wi.Request, ct).ConfigureAwait(false);

                try
                {
                    await WithTimeout(wi.Tcs.Task, wi.TimeoutMs > 0 ? wi.TimeoutMs : _opt.DefaultTimeoutMs, wi.Ct)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException tex)
                {
                    if (ReferenceEquals(Interlocked.CompareExchange(ref _inflight, null, inflight), inflight))
                        wi.Fail(tex);
                    if (_opt.AutoReconnect)
                        await ReconnectHardAsync("timeout hard-resync", ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce)
                {
                    if (ReferenceEquals(Interlocked.CompareExchange(ref _inflight, null, inflight), inflight))
                        wi.Fail(oce);
                }
                catch (Exception ex)
                {
                    RaiseFault(ex, "RequestLoop");
                    if (ReferenceEquals(Interlocked.CompareExchange(ref _inflight, null, inflight), inflight))
                        wi.Fail(ex);

                    if (_opt.AutoReconnect)
                        await ReconnectHardAsync("request exception", ct).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task WatchdogLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_opt.WatchdogPeriodMs, ct).ConfigureAwait(false);
            if (!PortOpen) continue;

            var infl = Volatile.Read(ref _inflight);
            if (infl == null) continue;

            var sinceRead = DateTime.UtcNow - _lastReadUtc;
            if (sinceRead.TotalMilliseconds >= _opt.StallReadMs)
            {
                await ReconnectHardAsync("watchdog: port open but no reads", ct).ConfigureAwait(false);
            }
        }
    }
    private async Task<bool[]> ReadBitsAsync(byte slave, byte fc, ushort start, ushort qty, int timeoutMs, CancellationToken ct)
    {
        if (qty == 0) throw new ArgumentOutOfRangeException(nameof(qty));
        var req = BuildReadRequest(slave, fc, start, qty);
        var matcher = InflightMatcher.ForReadBits(slave, fc, qty);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        using var lease = new FrameLease(respPb);

        var view = new ModbusFrameView(lease.Memory);
        ThrowIfModbusException(view);
        var payload = view.Payload;
        if (payload.Length < 1) throw new FormatException("Bad response payload");

        int byteCount = payload.Span[0];
        if (payload.Length < 1 + byteCount) throw new FormatException("Short payload");

        var data = payload.Slice(1, byteCount);
        bool[] bits = new bool[qty];

        for (int i = 0; i < qty; i++)
        {
            int bi = i >> 3;
            int bit = i & 7;
            bits[i] = bi < data.Length && ((data.Span[bi] >> bit) & 0x01) != 0;
        }

        return bits;
    }

    public async Task<ushort[]> ReadRegistersAsync(byte slave, byte fc, ushort start, ushort qty, int timeoutMs, CancellationToken ct)
    {
        if (qty == 0) throw new ArgumentOutOfRangeException(nameof(qty));
        var req = BuildReadRequest(slave, fc, start, qty);
        var matcher = InflightMatcher.ForReadRegisters(slave, fc, qty);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        using var lease = new FrameLease(respPb);

        var view = new ModbusFrameView(lease.Memory);
        ThrowIfModbusException(view);

        var payload = view.Payload; // [byteCount][hi][lo]...
        if (payload.Length < 1) throw new FormatException("Bad response payload");

        int byteCount = payload.Span[0];
        if (byteCount != qty * 2) throw new FormatException("ByteCount mismatch");

        if (payload.Length < 1 + byteCount) throw new FormatException("Short payload");

        var regs = new ushort[qty];
        int off = 1;
        for (int i = 0; i < qty; i++)
        {
            int idx = off + (i * 2);
            regs[i] = (ushort)((payload.Span[idx] << 8) | payload.Span[idx + 1]);
        }
        return regs;
    }

    private async Task WriteSingleCoilCoreAsync(byte slave, ushort coil, bool value, int timeoutMs, CancellationToken ct)
    {
        var req = BuildFc05(slave, coil, value);
        var matcher = InflightMatcher.ForFc05(slave, coil, value);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        using var lease = new FrameLease(respPb);

        var view = new ModbusFrameView(lease.Memory);
        ThrowIfModbusException(view);
    }

    private async Task WriteSingleRegisterCoreAsync(byte slave, ushort reg, ushort value, int timeoutMs, CancellationToken ct)
    {
        var req = BuildFc06(slave, reg, value);
        var matcher = InflightMatcher.ForFc06(slave, reg, value);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        using var lease = new FrameLease(respPb);

        var view = new ModbusFrameView(lease.Memory);
        ThrowIfModbusException(view);
    }

    private async Task WriteMultipleCoilsCoreAsync(byte slave, ushort start, bool[] values, int timeoutMs, CancellationToken ct)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (values.Length == 0) throw new ArgumentOutOfRangeException(nameof(values));

        var req = BuildFc0F(slave, start, values);
        var matcher = InflightMatcher.ForFc0F(slave, start, (ushort)values.Length);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        using var lease = new FrameLease(respPb);

        var view = new ModbusFrameView(lease.Memory);
        ThrowIfModbusException(view);
    }

    private async Task WriteMultipleRegistersCoreAsync(byte slave, ushort start, ushort[] values, int timeoutMs, CancellationToken ct)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (values.Length == 0) throw new ArgumentOutOfRangeException(nameof(values));

        var req = BuildFc10(slave, start, values);
        var matcher = InflightMatcher.ForFc10(slave, start, (ushort)values.Length);

        var respPb = await EnqueueRequestAndWaitAsync(req, matcher, timeoutMs, ct).ConfigureAwait(false);
        using var lease = new FrameLease(respPb);

        var view = new ModbusFrameView(lease.Memory);
        ThrowIfModbusException(view);
    }

    private async Task<PooledBuffer> EnqueueRequestAndWaitAsync(PooledBuffer request, InflightMatcher matcher, int timeoutMs, CancellationToken ct)
    {
        if (!IsRunning) throw new InvalidOperationException("Client not started.");

        var tcs = new TaskCompletionSource<PooledBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var wi = new RequestWorkItem(request, matcher, timeoutMs <= 0 ? _opt.DefaultTimeoutMs : timeoutMs, ct, tcs);

        await _reqQ.Writer.WriteAsync(wi, ct).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    private static async Task WithTimeout(Task task, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0) timeoutMs = 800;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var delay = Task.Delay(timeoutMs, cts.Token);

        var done = await Task.WhenAny(task, delay).ConfigureAwait(false);
        if (done == delay)
            throw new TimeoutException($"Timeout after {timeoutMs}ms");

        cts.Cancel();
        await task.ConfigureAwait(false);
    }

    private static void ThrowIfModbusException(ModbusFrameView view)
    {
        if (!view.IsException) return;
        throw new IOException($"Modbus exception: slave={view.Address}, fc=0x{(view.Function & 0x7F):X2}, code=0x{view.ExceptionCode:X2}");
    }
    internal sealed class PooledBuffer : IDisposable
    {
        private byte[]? _arr;
        private readonly ArrayPool<byte> _pool;

        public int Length { get; private set; }

        internal PooledBuffer(int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            _pool = ArrayPool<byte>.Shared;
            _arr = _pool.Rent(length);
            Length = length;
        }
        internal byte[] Buffer => _arr ?? System.Array.Empty<byte>();

        internal Span<byte> Span => _arr == null ? Span<byte>.Empty : _arr.AsSpan(0, Length);

        internal ReadOnlyMemory<byte> ReadOnlyMemory =>
            _arr == null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(_arr, 0, Length);

        public void Dispose()
        {
            var a = _arr;
            _arr = null;

            if (a != null) _pool.Return(a);
            Length = 0;
        }
    }

    private readonly struct ModbusFrameView
    {
        private readonly ReadOnlyMemory<byte> _mem;
        public ModbusFrameView(ReadOnlyMemory<byte> frame) => _mem = frame;

        public ReadOnlySpan<byte> Raw => _mem.Span;
        public int Length => _mem.Length;

        public byte Address => Raw.Length >= 1 ? Raw[0] : (byte)0;
        public byte Function => Raw.Length >= 2 ? Raw[1] : (byte)0;

        public bool IsException => (Function & 0x80) != 0;
        public byte ExceptionCode => Raw.Length >= 3 ? Raw[2] : (byte)0;
        public ReadOnlyMemory<byte> Payload
        {
            get
            {
                if (_mem.Length < 4) return ReadOnlyMemory<byte>.Empty;
                return _mem.Slice(2, _mem.Length - 4);
            }
        }
    }

    private readonly struct InflightMatcher
    {
        private readonly byte _addr;
        private readonly byte _fc;
        private readonly RawMatchKind _kind;

        private readonly int _expectedByteCount; 
        private readonly ushort _start;
        private readonly ushort _qty;
        private readonly ushort _echo; 

        private InflightMatcher(byte addr, byte fc, RawMatchKind kind, int expectedByteCount, ushort start, ushort qty, ushort echo)
        {
            _addr = addr;
            _fc = fc;
            _kind = kind;
            _expectedByteCount = expectedByteCount;
            _start = start;
            _qty = qty;
            _echo = echo;
        }

        public static InflightMatcher ForReadBits(byte addr, byte fc, ushort qty)
            => new(addr, fc, RawMatchKind.ReadBits, (qty + 7) / 8, 0, qty, 0);

        public static InflightMatcher ForReadRegisters(byte addr, byte fc, ushort qty)
            => new(addr, fc, RawMatchKind.ReadRegisters, qty * 2, 0, qty, 0);

        public static InflightMatcher ForFc05(byte addr, ushort coil, bool value)
            => new(addr, 0x05, RawMatchKind.Fc05, 0, coil, 0, value ? (ushort)0xFF00 : (ushort)0x0000);

        public static InflightMatcher ForFc06(byte addr, ushort reg, ushort value)
            => new(addr, 0x06, RawMatchKind.Fc06, 0, reg, 0, value);

        public static InflightMatcher ForFc0F(byte addr, ushort start, ushort qty)
            => new(addr, 0x0F, RawMatchKind.Fc0F, 0, start, qty, 0);

        public static InflightMatcher ForFc10(byte addr, ushort start, ushort qty)
            => new(addr, 0x10, RawMatchKind.Fc10, 0, start, qty, 0);

        public static InflightMatcher FromRaw(byte addr, byte fc, RawMatchKind kind, ushort startOrAddr, ushort qty, ushort echo)
        {
            int expectedByteCount = kind switch
            {
                RawMatchKind.ReadBits => (qty + 7) / 8,
                RawMatchKind.ReadRegisters => qty * 2,
                _ => 0
            };
            return new InflightMatcher(addr, fc, kind, expectedByteCount, startOrAddr, qty, echo);
        }

        public bool IsMatch(ModbusFrameView f)
        {
            if (f.Address != _addr) return false;
            if (f.IsException)
                return (byte)(f.Function & 0x7F) == _fc;

            if (f.Function != _fc) return false;

            var p = f.Payload;
            switch (_kind)
            {
                case RawMatchKind.ReadBits:
                case RawMatchKind.ReadRegisters:
                    if (p.Length < 1) return false;
                    return p.Span[0] == _expectedByteCount;

                case RawMatchKind.Fc05:
                case RawMatchKind.Fc06:
                    if (p.Length != 4) return false;
                    ushort addr = (ushort)((p.Span[0] << 8) | p.Span[1]);
                    ushort val = (ushort)((p.Span[2] << 8) | p.Span[3]);
                    return addr == _start && val == _echo;

                case RawMatchKind.Fc0F:
                case RawMatchKind.Fc10:
                    if (p.Length != 4) return false;
                    ushort st = (ushort)((p.Span[0] << 8) | p.Span[1]);
                    ushort qt = (ushort)((p.Span[2] << 8) | p.Span[3]);
                    return st == _start && qt == _qty;

                default:
                    return true;
            }
        }
    }

    private sealed class Inflight
    {
        public InflightMatcher Matcher { get; }
        private readonly TaskCompletionSource<PooledBuffer> _tcs;

        public Inflight(InflightMatcher matcher, TaskCompletionSource<PooledBuffer> tcs)
        {
            Matcher = matcher;
            _tcs = tcs;
        }

        public void TryComplete(PooledBuffer pb) => _tcs.TrySetResult(pb);
        public void TryFail(Exception ex) => _tcs.TrySetException(ex);
    }

    private sealed class RequestWorkItem
    {
        public PooledBuffer Request { get; }
        public InflightMatcher Matcher { get; }
        public int TimeoutMs { get; }
        public CancellationToken Ct { get; }
        public TaskCompletionSource<PooledBuffer> Tcs { get; }

        public RequestWorkItem(PooledBuffer request, InflightMatcher matcher, int timeoutMs, CancellationToken ct, TaskCompletionSource<PooledBuffer> tcs)
        {
            Request = request;
            Matcher = matcher;
            TimeoutMs = timeoutMs;
            Ct = ct;
            Tcs = tcs;
        }

        public void Fail(Exception ex)
        {
            try { Tcs.TrySetException(ex); } catch { }
            try { Request.Dispose(); } catch { }
        }
    }
    private sealed class ModbusRtuExtractor
    {
        private const int MaxFrameLen = 256;
        private readonly ByteRingBuffer _rb;

        public ModbusRtuExtractor(int capacityBytes)
        {
            if (capacityBytes < 4096) capacityBytes = 4096;
            _rb = new ByteRingBuffer(capacityBytes);
        }

        public void Reset() => _rb.Clear();

        public void Push(ReadOnlySpan<byte> bytes)
        {
            if (!bytes.IsEmpty) _rb.Write(bytes);
        }

        public bool TryPop(out PooledBuffer frame)
        {
            frame = null!;

            while (_rb.Count >= 5)
            {
                if (!_rb.TryPeek(0, out byte addr) || !_rb.TryPeek(1, out byte fc))
                    return false;

                if (addr == 0 || addr > 247)
                {
                    _rb.Skip(1);
                    continue;
                }

                int len;
                if ((fc & 0x80) != 0)
                {
                    len = 5;
                }
                else
                {
                    switch (fc)
                    {
                        case 0x01:
                        case 0x02:
                        case 0x03:
                        case 0x04:
                            if (_rb.Count < 3) return false;
                            if (!_rb.TryPeek(2, out byte bc)) return false;
                            if (bc > 252) { _rb.Skip(1); continue; }
                            len = 5 + bc;
                            break;

                        case 0x05:
                        case 0x06:
                        case 0x0F:
                        case 0x10:
                            len = 8; 
                            break;

                        default:
                            _rb.Skip(1);
                            continue;
                    }
                }

                if (len < 5 || len > MaxFrameLen)
                {
                    _rb.Skip(1);
                    continue;
                }

                if (_rb.Count < len) return false;

                var pb = new PooledBuffer(len);
                _rb.CopyOut(0, len, pb.Span);

                ushort got = (ushort)(pb.Span[^2] | (pb.Span[^1] << 8));
                ushort calc = ModbusCrc16.Compute(pb.Span.Slice(0, len - 2));
                if (got != calc)
                {
                    pb.Dispose();
                    _rb.Skip(1);
                    continue;
                }

                _rb.Skip(len);
                frame = pb;
                return true;
            }

            return false;
        }
    }

    private sealed class ByteRingBuffer
    {
        private byte[] _buf;
        private int _head, _tail, _count;
        public int Count => _count;

        public ByteRingBuffer(int capacity)
        {
            if (capacity < 256) capacity = 256;
            _buf = new byte[capacity];
        }

        public void Clear() => _head = _tail = _count = 0;

        private void Ensure(int add)
        {
            int need = _count + add;
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
            Ensure(data.Length);

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

    private static class ModbusCrc16
    {
        public static ushort Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int b = 0; b < 8; b++)
                {
                    bool lsb = (crc & 1) != 0;
                    crc >>= 1;
                    if (lsb) crc ^= 0xA001;
                }
            }
            return crc;
        }
    }
    private static PooledBuffer BuildReadRequest(byte addr, byte fc, ushort start, ushort qty)
    {
        var pb = new PooledBuffer(8);
        var s = pb.Span;

        s[0] = addr;
        s[1] = fc;
        s[2] = (byte)(start >> 8);
        s[3] = (byte)(start);
        s[4] = (byte)(qty >> 8);
        s[5] = (byte)(qty);

        ushort crc = ModbusCrc16.Compute(s.Slice(0, 6));
        s[6] = (byte)(crc & 0xFF);
        s[7] = (byte)(crc >> 8);
        return pb;
    }

    private static PooledBuffer BuildFc05(byte addr, ushort coil, bool value)
    {
        var pb = new PooledBuffer(8);
        var s = pb.Span;

        s[0] = addr;
        s[1] = 0x05;
        s[2] = (byte)(coil >> 8);
        s[3] = (byte)(coil);

        ushort v = value ? (ushort)0xFF00 : (ushort)0x0000;
        s[4] = (byte)(v >> 8);
        s[5] = (byte)(v);

        ushort crc = ModbusCrc16.Compute(s.Slice(0, 6));
        s[6] = (byte)(crc & 0xFF);
        s[7] = (byte)(crc >> 8);
        return pb;
    }

    private static PooledBuffer BuildFc06(byte addr, ushort reg, ushort value)
    {
        var pb = new PooledBuffer(8);
        var s = pb.Span;

        s[0] = addr;
        s[1] = 0x06;
        s[2] = (byte)(reg >> 8);
        s[3] = (byte)(reg);
        s[4] = (byte)(value >> 8);
        s[5] = (byte)(value);

        ushort crc = ModbusCrc16.Compute(s.Slice(0, 6));
        s[6] = (byte)(crc & 0xFF);
        s[7] = (byte)(crc >> 8);
        return pb;
    }

    private static PooledBuffer BuildFc0F(byte addr, ushort start, bool[] values)
    {
        int qty = values.Length;
        int byteCount = (qty + 7) / 8;
        int len = 9 + byteCount;

        var pb = new PooledBuffer(len);
        var s = pb.Span;

        s[0] = addr;
        s[1] = 0x0F;
        s[2] = (byte)(start >> 8);
        s[3] = (byte)(start);
        s[4] = (byte)(qty >> 8);
        s[5] = (byte)(qty);
        s[6] = (byte)byteCount;

        var data = s.Slice(7, byteCount);
        data.Clear();
        for (int i = 0; i < qty; i++)
        {
            if (values[i])
                data[i >> 3] |= (byte)(1 << (i & 7)); 
        }

        ushort crc = ModbusCrc16.Compute(s.Slice(0, len - 2));
        s[len - 2] = (byte)(crc & 0xFF);
        s[len - 1] = (byte)(crc >> 8);
        return pb;
    }

    private static PooledBuffer BuildFc10(byte addr, ushort start, ushort[] values)
    {
        int qty = values.Length;
        int byteCount = qty * 2;
        int len = 9 + byteCount;

        var pb = new PooledBuffer(len);
        var s = pb.Span;

        s[0] = addr;
        s[1] = 0x10;
        s[2] = (byte)(start >> 8);
        s[3] = (byte)(start);
        s[4] = (byte)(qty >> 8);
        s[5] = (byte)(qty);
        s[6] = (byte)byteCount;

        int off = 7;
        for (int i = 0; i < qty; i++)
        {
            ushort v = values[i];
            s[off++] = (byte)(v >> 8);
            s[off++] = (byte)(v);
        }

        ushort crc = ModbusCrc16.Compute(s.Slice(0, len - 2));
        s[len - 2] = (byte)(crc & 0xFF);
        s[len - 1] = (byte)(crc >> 8);
        return pb;
    }
}
