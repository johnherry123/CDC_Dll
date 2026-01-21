using System.Collections.Concurrent;
using System.Threading.Channels;
using CDC_Dll.Core.Abstractions.Diagnostics;
using CDC_Dll.Core.Abstractions.Protocol;
using CDC_Dll.Core.Abstractions.Transport;
using CDC_Dll.Core.Application.Errors;
using CDC_Dll.Core.Domain.Errors;
using CDC_Dll.Core.Domain.Models;
using CDC_Dll.Core.Domain.Protocol;
using CDC_Dll.Core.Infrastructure.Client;
using CDC_Dll.Core.Infrastructure.Exceptions;
using CDC_DLL.Core.Abstractions.Protocol;


namespace CDC_DLL.Core.Infrastructure.Client;

public sealed class ProtocolClient : IProtocolClient
{
    private readonly ITransport _transport;
    private readonly IFrameCodec _codec;
    private readonly IErrorBus _errorBus;
    private readonly ProtocolClientOptions _options;

   
    private readonly Channel<byte[]> _txHigh;
    private readonly Channel<byte[]> _txLow;

  
    private readonly Channel<Frame> _telemetryCh;


    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<Frame>> _pending = new();

    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private Task? _writeTask;
    private Task? _telemetryTask;
    private Task? _aliveTask;
    private Task? _healthTask;

    private ushort _nextMsgId = 1;

    private DateTime _startUtc = DateTime.UtcNow;
    private DateTime _lastRxUtc;
    private DateTime _lastTxUtc;

    private long _rxBytes, _txBytes, _rxFrames, _txFrames;
    private long _crcFailures, _seqMisses, _cmdTimeouts;

    private int _consecutiveTimeouts;
    private string? _lastError;

    private ConnectionState _state = ConnectionState.Disconnected;

    
    public bool isRunning => _cts is not null && !_cts.IsCancellationRequested;

    public ConnectionHealthSnapshot Health => GetConnectionHealthSnapshot();

    public event Action<Frame>? TelemetryReceived;
    public event Action<ErrorInfo>? ErrorRaised;

 
    public ProtocolClient(ITransport transport, IFrameCodec codec, IErrorBus errorBus)
        : this(transport, codec, errorBus, new ProtocolClientOptions())
    {
    }

    public ProtocolClient(ITransport transport, IFrameCodec codec, IErrorBus errorBus, ProtocolClientOptions options)
    {
        _transport = transport;
        _codec = codec;
        _errorBus = errorBus;
        _options = options ?? new ProtocolClientOptions();

        _txHigh = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(_options.TxQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _txLow = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(_options.TxQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest 
        });

        _telemetryCh = Channel.CreateBounded<Frame>(new BoundedChannelOptions(_options.TelemetryQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }


    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        if (isRunning) return;

        _codec.Reset();
        _startUtc = DateTime.UtcNow;
        _lastRxUtc = default;
        _lastTxUtc = default;
        _consecutiveTimeouts = 0;
        _lastError = null;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _state = ConnectionState.Connecting;
            await _transport.OpenAsync(_cts.Token).ConfigureAwait(false);
            _state = ConnectionState.Connected;

            _readTask = Task.Run(ReadLoop, _cts.Token);
            _writeTask = Task.Run(WriteLoop, _cts.Token);
            _telemetryTask = Task.Run(TelemetryLoop, _cts.Token);

            if (_options.EnableAlive)
                _aliveTask = Task.Run(AliveLoop, _cts.Token);

            _healthTask = Task.Run(HealthLoop, _cts.Token);
        }
        catch (Exception ex)
        {
            _state = ConnectionState.Faulted;
            PublishError(ex, "StartAsync", "Unable to establish a connection.");
            throw;
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null) return;

        try
        {
            _cts.Cancel();

        
            foreach (var kv in _pending)
                kv.Value.TrySetException(new TransportException(ErrorCode.TransportDisconnected, "Client stopped"));

            _pending.Clear();

            if (_readTask is not null) await SafeAwait(_readTask).ConfigureAwait(false);
            if (_writeTask is not null) await SafeAwait(_writeTask).ConfigureAwait(false);
            if (_telemetryTask is not null) await SafeAwait(_telemetryTask).ConfigureAwait(false);
            if (_aliveTask is not null) await SafeAwait(_aliveTask).ConfigureAwait(false);
            if (_healthTask is not null) await SafeAwait(_healthTask).ConfigureAwait(false);
        }
        finally
        {
            try { await _transport.CloseAsync(cancellationToken).ConfigureAwait(false); } catch { }

            _state = ConnectionState.Disconnected;

            _cts.Dispose();
            _cts = null;
        }
    }

    public async Task<Frame> SendCommandAsync(Frame commandFrame, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!isRunning)
            throw new TransportException(ErrorCode.TransportDisconnected, "Client not running.");

        var msgId = AllocateMsgId();

     
        var hdr = new FrameHeader(
            Version: commandFrame.Header.Version,
            Type: MsgType.Command,
            MsgId: msgId,
            Seq: commandFrame.Header.Seq,
            PayloadLength: commandFrame.Header.PayloadLength);

        var frame = new Frame(hdr, commandFrame.Payload);

        var tcs = new TaskCompletionSource<Frame>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(msgId, tcs))
            throw new ProtocolException(ErrorCode.InternalError, "MsgId collision.");

        var bytes = _codec.Encode(frame);

        await _txHigh.Writer.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);

        Interlocked.Increment(ref _txFrames);
        Interlocked.Add(ref _txBytes, bytes.Length);
        _lastTxUtc = DateTime.UtcNow;

        var effTimeout = timeout <= TimeSpan.Zero ? _options.DefaultCommandTimeout : timeout;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(effTimeout);

        try
        {
            var resp = await tcs.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            _consecutiveTimeouts = 0;
            return resp;
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _cmdTimeouts);
            _consecutiveTimeouts++;

            _pending.TryRemove(msgId, out _);
            throw new TimeoutException($"Command timeout MsgId={msgId}");
        }
    }

    public LinkMetricsSnapshot GetLinkMetricsSnapshot()
    {
        var now = DateTime.UtcNow;
        var secs = Math.Max(1.0, (now - _startUtc).TotalSeconds);

        var rxB = Interlocked.Read(ref _rxBytes);
        var txB = Interlocked.Read(ref _txBytes);
        var rxF = Interlocked.Read(ref _rxFrames);
        var txF = Interlocked.Read(ref _txFrames);

        return new LinkMetricsSnapshot(
            RxByte: rxB,
            TxByte: txB,
            RxFrame: rxF,
            TxFrame: txF,
            CRCFailure: Interlocked.Read(ref _crcFailures),
            SeqMisses: Interlocked.Read(ref _seqMisses),
            CommandTimeouts: Interlocked.Read(ref _cmdTimeouts),
            PendingRequests: _pending.Count,
            LastRxUtc: _lastRxUtc,
            LastTxUtc: _lastTxUtc,
            RxBytesPerSec: rxB / secs,
            TxBytesPerSec: txB / secs,
            RxFramesPerSec: rxF / secs
        );
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        await _transport.DisposeAsync().ConfigureAwait(false);
    }



    private async Task ReadLoop()
    {
        var buf = new byte[8192];

        try
        {
            while (_cts is not null && !_cts.IsCancellationRequested)
            {
                var n = await _transport.ReadAsync(buf, _cts.Token).ConfigureAwait(false);
                if (n <= 0) continue;

                Interlocked.Add(ref _rxBytes, n);
                _lastRxUtc = DateTime.UtcNow;

                IEnumerable<Frame> frames;
                try
                {
                    frames = _codec.Feed(buf.AsSpan(0, n));
                }
                catch (ProtocolException pex)
                {
                    if (pex.Code == ErrorCode.ProtocolCRCFailed)
                        Interlocked.Increment(ref _crcFailures);

                    PublishError(pex, "Decode", "Error Acknowledgment Data (CRC/Frame).");
                    continue; 
                }
                catch (Exception ex)
                {
                    PublishError(ex, "Decode", "Data decryption error.");
                    continue;
                }

                foreach (var f in frames)
                {
                    Interlocked.Increment(ref _rxFrames);

                    if (f.Header.Type == MsgType.Response && f.Header.MsgId != 0)
                    {
                        if (_pending.TryRemove(f.Header.MsgId, out var tcs))
                            tcs.TrySetResult(f);
                    }
                    else if (f.Header.Type == MsgType.Telemetry || f.Header.Type == MsgType.Event)
                    {
                        _telemetryCh.Writer.TryWrite(f);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _state = ConnectionState.Faulted;
            PublishError(ex, "ReadLoop", "Disconnected while reading.");
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task WriteLoop()
    {
        try
        {
            while (_cts is not null && !_cts.IsCancellationRequested)
            {
              
                if (_txHigh.Reader.TryRead(out var hi))
                {
                    await _transport.WriteAsync(hi, _cts.Token).ConfigureAwait(false);
                    _lastTxUtc = DateTime.UtcNow;
                    continue;
                }

           
                var waitHigh = _txHigh.Reader.WaitToReadAsync(_cts.Token).AsTask();
                var waitLow = _txLow.Reader.WaitToReadAsync(_cts.Token).AsTask();
                var completed = await Task.WhenAny(waitHigh, waitLow).ConfigureAwait(false);

                if (completed == waitHigh && await waitHigh.ConfigureAwait(false))
                    continue;

                if (completed == waitLow && await waitLow.ConfigureAwait(false))
                {
                    if (_txLow.Reader.TryRead(out var lo))
                    {
                        await _transport.WriteAsync(lo, _cts.Token).ConfigureAwait(false);
                        _lastTxUtc = DateTime.UtcNow;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _state = ConnectionState.Faulted;
            PublishError(ex, "WriteLoop", "Connection lost while sending.");
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task TelemetryLoop()
    {
        try
        {
            while (_cts is not null && await _telemetryCh.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (_telemetryCh.Reader.TryRead(out var f))
                {
                    TelemetryReceived?.Invoke(f);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PublishError(ex, "TelemetryLoop", "Lỗi xử lý telemetry.");
        }
    }

    private async Task AliveLoop()
    {
        try
        {
            while (_cts is not null && !_cts.IsCancellationRequested)
            {
                await Task.Delay(_options.AliveInterval, _cts.Token).ConfigureAwait(false);
                var hdr = new FrameHeader(
                    Version: ProtocolVersion.Current,
                    Type: MsgType.Alive,
                    MsgId: 0,
                    Seq: 0,
                    PayloadLength: 0);

                var alive = new Frame(hdr, ReadOnlyMemory<byte>.Empty);
                var bytes = _codec.Encode(alive);

                _txLow.Writer.TryWrite(bytes);

                Interlocked.Increment(ref _txFrames);
                Interlocked.Add(ref _txBytes, bytes.Length);
                _lastTxUtc = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            PublishError(ex, "AliveLoop", "Error sending alive.");
        }
    }

    private async Task HealthLoop()
    {
        try
        {
            while (_cts is not null && !_cts.IsCancellationRequested)
            {
                await Task.Delay(200, _cts.Token).ConfigureAwait(false);

                if (_lastRxUtc == default) continue;

                var sinceRx = DateTime.UtcNow - _lastRxUtc;
                if (sinceRx >= _options.RxFaultAfter)
                {
                    _state = ConnectionState.Faulted;

                    PublishError(
                        new TimeoutException($"No RX for {sinceRx.TotalMilliseconds:0}ms"),
                        "Health",
                        "Connection lost (no data received)"
                    );

                    await StopAsync(CancellationToken.None).ConfigureAwait(false);
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            PublishError(ex, "HealthLoop", "Health care error.");
        }
    }

  

    private ushort AllocateMsgId()
    {
        var id = _nextMsgId++;
        if (_nextMsgId == 0) _nextMsgId = 1;
        return id;
    }

    private ConnectionHealthSnapshot GetConnectionHealthSnapshot()
    {
        var sinceRx = _lastRxUtc == default ? TimeSpan.MaxValue : DateTime.UtcNow - _lastRxUtc;
        return new ConnectionHealthSnapshot(_state, _consecutiveTimeouts, sinceRx, _lastError);
    }

    private void PublishError(Exception ex, string op, string userMessage)
    {
        _lastError = ex.Message;

        var ctx = ResultGuard.ctx(op, component: nameof(ProtocolClient));
        var err = ErrorMapper.FromException(ex, ctx, userMessage);

        _errorBus.Publish(err);
        ErrorRaised?.Invoke(err);
    }

    private static async Task SafeAwait(Task t)
    {
        try { await t.ConfigureAwait(false); }
        catch { }
    }
}
