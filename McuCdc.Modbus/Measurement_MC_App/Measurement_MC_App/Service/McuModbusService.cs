using McuCdc.Modbus;
using Measurement_MC_App.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Measurement_MC_App.Service
{
    public sealed class McuModbusService : IMcuModbusService
    {
        private const int DefaultPulseMs = 100;

        private readonly SemaphoreSlim _ioGate = new(1, 1);
        private readonly McuStatus _st = new();

        private readonly SynchronizationContext? _uiCtx;

        private ModbusCdcClient? _client;
        private byte _unitId;
        private volatile bool _connected;

        private CancellationTokenSource? _listenCts;
        private Task? _listenTask;

        public bool EnablePollingLog { get; set; } = false;

        public int IdlePollMs { get; set; } = 300;
        public int MovingPollMs { get; set; } = 80;
        public int ErrorPollMs { get; set; } = 150;

        public int IoTimeoutMs { get; set; } = 0;

        public bool IsConnected => _connected;

        public event Action<bool>? ConnectionChanged;
        public event Action<LogItem>? LogProduced;
        public event Action<McuStatus>? StatusChanged;

        public McuModbusService()
        {
            _uiCtx = SynchronizationContext.Current;
        }

        public async Task ConnectAsync(string portName, int baudRate, byte unitId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(portName)) throw new ArgumentException("PortName is required.", nameof(portName));
            if (baudRate <= 0) throw new ArgumentOutOfRangeException(nameof(baudRate));

            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_connected) return;

                _unitId = unitId;

                var opt = new ModbusCdcClient.Options
                {
                    PortName = portName,
                    BaudRate = baudRate,

                    Addressing = ModbusCdcClient.AddressingMode.OneBasedDisplay,

                    DefaultTimeoutMs = 800,
                    AutoReconnect = true,
                    ReconnectDelayMs = 300,
                    WatchdogPeriodMs = 500,
                    StallReadMs = 2500,

                    ReadChunkBytes = 4096,
                    RingBufferBytes = 16 * 1024,
                    MaxQueuedRequests = 256,
                    RxQueueCapacity = 1024,
                    TxQueueCapacity = 256,
                };

                _client = new ModbusCdcClient(opt);
                _client.Faulted += (ex, where) => Emit(LogDir.Error, "Client faulted", $"{where}: {ex.Message}");
                _client.UnsolicitedFrame += mem => Emit(LogDir.Warn, "UnsolicitedFrame", $"len={mem.Length}");

                Emit(LogDir.Info, "Connect", $"{portName}@{baudRate} UnitId={unitId}");
                await _client.StartAsync(ct).ConfigureAwait(false);

                _connected = true;
            }
            finally { _ioGate.Release(); }

            RaiseOnUI(() => ConnectionChanged?.Invoke(true));
            Emit(LogDir.Info, "Connected");

            await StartListeningAsync(ct).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(CancellationToken ct = default)
        {
            await StopListeningAsync(ct).ConfigureAwait(false);

            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!_connected) return;

                try { await JogStopAllAsync(ct).ConfigureAwait(false); }
                catch (Exception ex) { Emit(LogDir.Warn, "JogStopAll failed", ex.Message); }

                if (_client is not null)
                {
                    Emit(LogDir.Info, "Disconnect");
                    try { await _client.StopAsync().ConfigureAwait(false); } catch { /* ignore */ }
                    try { await _client.DisposeAsync().ConfigureAwait(false); } catch { /* ignore */ }
                    _client = null;
                }

                _connected = false;
            }
            finally { _ioGate.Release(); }

            RaiseOnUI(() => ConnectionChanged?.Invoke(false));
            Emit(LogDir.Info, "Disconnected");
        }

        public Task StartListeningAsync(CancellationToken ct = default)
        {
            if (!_connected) throw new InvalidOperationException("Not connected.");

            if (_listenTask is not null && !_listenTask.IsCompleted) return Task.CompletedTask;

            _listenCts?.Dispose();
            _listenCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var token = _listenCts.Token;
            _listenTask = Task.Run(() => ListenLoopAdaptive(token), token);

            Emit(LogDir.Info, "Listen started", "1x+3x adaptive");
            return Task.CompletedTask;
        }

        public async Task StopListeningAsync(CancellationToken ct = default)
        {
            var cts = _listenCts;
            var task = _listenTask;

            if (cts is null) return;

            cts.Cancel();

            try
            {
                if (task is not null)
                    await task.WaitAsync(ct).ConfigureAwait(false);
            }
            catch { }
            finally
            {
                _listenTask = null;
                _listenCts?.Dispose();
                _listenCts = null;
                Emit(LogDir.Info, "Listen stopped");
            }
        }

 
        private async Task ListenLoopAdaptive(CancellationToken token)
        {
            int failBackoffMs = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await PollTick_1x3x(token).ConfigureAwait(false);
                    failBackoffMs = 0;

                    await Task.Delay(ComputeNextDelay(), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Emit(LogDir.Warn, "Listen tick failed", ex.Message);

      
                    failBackoffMs = failBackoffMs == 0 ? 120 : Math.Min(1000, failBackoffMs + 120);
                    try { await Task.Delay(failBackoffMs, token).ConfigureAwait(false); }
                    catch { }
                }
            }
        }

        private int ComputeNextDelay()
        {
            int idle = Clamp(IdlePollMs, 80, 2000);
            int mov = Clamp(MovingPollMs, 20, 1000);
            int err = Clamp(ErrorPollMs, 50, 2000);

            if (_st.StateX == AxisRunState.ERROR || _st.StateY == AxisRunState.ERROR || _st.StateZ == AxisRunState.ERROR)
                return err;

            bool moving =
                _st.StateX != AxisRunState.STOP || _st.StateY != AxisRunState.STOP || _st.StateZ != AxisRunState.STOP ||
                _st.SpeedX != 0 || _st.SpeedY != 0 || _st.SpeedZ != 0;

            return moving ? mov : idle;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private async Task PollTick_1x3x(CancellationToken ct)
        {
            var client = _client;
            if (!_connected || client is null) return;

            bool[] in1x;
            ushort[] in3x;
            bool[] out0x;
            int timeout = IoTimeoutMs > 0 ? IoTimeoutMs : 800;


            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (EnablePollingLog) Emit(LogDir.Tx, "Read", "1x+3x");

                in1x = await client.ReadDiscreteInputsAsync(_unitId, 1, ModbusRanges.In1x_Count, IoTimeoutMs, ct).ConfigureAwait(false);
                in3x = await client.ReadInputRegistersAsync(_unitId, 1, ModbusRanges.In3x_Count, IoTimeoutMs, ct).ConfigureAwait(false);
                const ushort start = (ushort)Coil0xAddr.Output_1; 
                const ushort count = 16;                          
                out0x = await client.ReadCoilsAsync(_unitId, start, count, timeout, ct);
            }
            finally { _ioGate.Release(); }

            bool changed = false;

            for (int i = 0; i < ModbusRanges.In1x_Count; i++)
            {
                int idx = i + 1;
                bool v = in1x[i];
                if (_st.In1x[idx] != v) { _st.In1x[idx] = v; changed = true; }
            }

            changed |= SetU16(ref _st.PosX, in3x[0]);
            changed |= SetU16(ref _st.SpeedX, in3x[1]);
            changed |= SetAxis(ref _st.StateX, (AxisRunState)in3x[2]);

            changed |= SetU16(ref _st.PosY, in3x[3]);
            changed |= SetU16(ref _st.SpeedY, in3x[4]);
            changed |= SetAxis(ref _st.StateY, (AxisRunState)in3x[5]);

            changed |= SetU16(ref _st.PosZ, in3x[6]);
            changed |= SetU16(ref _st.SpeedZ, in3x[7]);
            changed |= SetAxis(ref _st.StateZ, (AxisRunState)in3x[8]);
            for (int i = 0; i < 18; i++)
            {
                ushort coilAddr = (ushort)(17 + i); 
                if (coilAddr >= 1 && coilAddr < _st.Coil0x.Length)
                {
                    bool v = out0x[i];
                    if (_st.Coil0x[coilAddr] != v) { _st.Coil0x[coilAddr] = v; changed = true; }
                }
            }

            if (changed)
            {
                _st.UpdatedAt = DateTime.Now;
                RaiseOnUI(() => StatusChanged?.Invoke(_st));
            }
        }

        private static bool SetU16(ref ushort dst, ushort value) { if (dst == value) return false; dst = value; return true; }
        private static bool SetAxis(ref AxisRunState dst, AxisRunState value) { if (dst == value) return false; dst = value; return true; }

        public Task WriteCoilAsync(Coil0xAddr addr, bool value, CancellationToken ct = default)
            => WriteCoilInternalAsync(addr, value, ct);

        public Task PulseCoilAsync(Coil0xAddr addr, int pulseMs = DefaultPulseMs, CancellationToken ct = default)
            => PulseCoilInternalAsync(addr, pulseMs, ct);

        public Task WriteRegisterAsync(Hold4xAddr addr, ushort value, CancellationToken ct = default)
            => WriteRegInternalAsync(addr, value, ct);

        private async Task WriteCoilInternalAsync(Coil0xAddr addr, bool value, CancellationToken ct)
        {
            var client = _client;
            if (!_connected || client is null) throw new InvalidOperationException("MCU not connected.");

            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                Emit(LogDir.Tx, $"0x Write {addr}", $"coil={(ushort)addr} val={(value ? 1 : 0)}");
                await client.WriteSingleCoilAsync(_unitId, (ushort)addr, value, IoTimeoutMs, ct).ConfigureAwait(false);
            }
            finally { _ioGate.Release(); }

    
            ushort a = (ushort)addr;
            if (a >= 1 && a <= ModbusRanges.Coil0x_Count) _st.Coil0x[a] = value;

            Emit(LogDir.Rx, $"0x OK {addr}", null);
        }

    
        private async Task PulseCoilInternalAsync(Coil0xAddr addr, int pulseMs, CancellationToken ct)
        {
            pulseMs = Clamp(pulseMs, 20, 5000);

       
            await WriteCoilInternalAsync(addr, true, ct).ConfigureAwait(false);

            try
            {

                await Task.Delay(pulseMs, ct).ConfigureAwait(false);
            }
            finally
            {

                try { await WriteCoilInternalAsync(addr, false, CancellationToken.None).ConfigureAwait(false); }
                catch (Exception ex) { Emit(LogDir.Warn, $"Pulse OFF failed {addr}", ex.Message); }
            }
        }

        private async Task WriteRegInternalAsync(Hold4xAddr addr, ushort value, CancellationToken ct)
        {
            var client = _client;
            if (!_connected || client is null) throw new InvalidOperationException("MCU not connected.");

            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                Emit(LogDir.Tx, $"4x Write {addr}", $"reg={(ushort)addr} val={value}");
                await client.WriteSingleRegisterAsync(_unitId, (ushort)addr, value, IoTimeoutMs, ct).ConfigureAwait(false);
            }
            finally { _ioGate.Release(); }

            // mirror local 4x (optional)
            switch (addr)
            {
                case Hold4xAddr.TargetX: _st.TargetX = value; break;
                case Hold4xAddr.SpeedTarX: _st.SpeedTarX = value; break;
                case Hold4xAddr.MaxX: _st.MaxX = value; break;
                case Hold4xAddr.TargetY: _st.TargetY = value; break;
                case Hold4xAddr.SpeedTarY: _st.SpeedTarY = value; break;
                case Hold4xAddr.MaxY: _st.MaxY = value; break;
                case Hold4xAddr.TargetZ: _st.TargetZ = value; break;
                case Hold4xAddr.SpeedTarZ: _st.SpeedTarZ = value; break;
                case Hold4xAddr.MaxZ: _st.MaxZ = value; break;
            }

            Emit(LogDir.Rx, $"4x OK {addr}", null);
        }

        public Task GoSensorHomeAsync(CancellationToken ct = default) => PulseCoilInternalAsync(Coil0xAddr.Go_Sensor_Home, DefaultPulseMs, ct);
        public Task SetPointAsync(CancellationToken ct = default) => PulseCoilInternalAsync(Coil0xAddr.Set_Point, DefaultPulseMs, ct);
        public Task EmergencyAsync(CancellationToken ct = default) => PulseCoilInternalAsync(Coil0xAddr.Emergency, DefaultPulseMs, ct);
        public Task RestartAsync(CancellationToken ct = default) => PulseCoilInternalAsync(Coil0xAddr.Restart, DefaultPulseMs, ct);
        public Task StopAsync(CancellationToken ct = default) => PulseCoilInternalAsync(Coil0xAddr.STOP, DefaultPulseMs, ct);

        public Task JogOxMinusDownAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Ox_Sub, true, ct);
        public Task JogOxMinusUpAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Ox_Sub, false, ct);

        public Task JogOxPlusDownAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Ox_Plus, true, ct);
        public Task JogOxPlusUpAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Ox_Plus, false, ct);

        public Task JogOyMinusDownAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oy_Sub, true, ct);
        public Task JogOyMinusUpAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oy_Sub, false, ct);

        public Task JogOyPlusDownAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oy_Plus, true, ct);
        public Task JogOyPlusUpAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oy_Plus, false, ct);

        public Task JogOzMinusDownAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oz_Sub, true, ct);
        public Task JogOzMinusUpAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oz_Sub, false, ct);

        public Task JogOzPlusDownAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oz_Plus, true, ct);
        public Task JogOzPlusUpAsync(CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Oz_Plus, false, ct);

        public async Task JogStopAllAsync(CancellationToken ct = default)
        {

            try { await WriteCoilInternalAsync(Coil0xAddr.Ox_Sub, false, ct).ConfigureAwait(false); } catch { }
            try { await WriteCoilInternalAsync(Coil0xAddr.Ox_Plus, false, ct).ConfigureAwait(false); } catch { }
            try { await WriteCoilInternalAsync(Coil0xAddr.Oy_Sub, false, ct).ConfigureAwait(false); } catch { }
            try { await WriteCoilInternalAsync(Coil0xAddr.Oy_Plus, false, ct).ConfigureAwait(false); } catch { }
            try { await WriteCoilInternalAsync(Coil0xAddr.Oz_Sub, false, ct).ConfigureAwait(false); } catch { }
            try { await WriteCoilInternalAsync(Coil0xAddr.Oz_Plus, false, ct).ConfigureAwait(false); } catch { }
        }

        public Task Output1Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_1, on, ct);
        public Task Output2Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_2, on, ct);
        public Task Output3Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_3, on, ct);
        public Task Output4Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_4, on, ct);
        public Task Output5Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_5, on, ct);
        public Task Output6Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_6, on, ct);
        public Task Output7Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_7, on, ct);
        public Task Output8Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_8, on, ct);
        public Task Output9Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_9, on, ct);
        public Task Output10Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_10, on, ct);
        public Task Output11Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_11, on, ct);
        public Task Output12Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_12, on, ct);
        public Task Output13Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_13, on, ct);
        public Task Output14Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_14, on, ct);
        public Task Output15Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_15, on, ct);
        public Task Output16Async(bool on, CancellationToken ct = default) => WriteCoilInternalAsync(Coil0xAddr.Output_16, on, ct);

        public Task SetTargetXAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.TargetX, value, ct);
        public Task SetSpeedTarXAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.SpeedTarX, value, ct);
        public Task SetMaxXAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.MaxX, value, ct);

        public Task SetTargetYAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.TargetY, value, ct);
        public Task SetSpeedTarYAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.SpeedTarY, value, ct);
        public Task SetMaxYAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.MaxY, value, ct);

        public Task SetTargetZAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.TargetZ, value, ct);
        public Task SetSpeedTarZAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.SpeedTarZ, value, ct);
        public Task SetMaxZAsync(ushort value, CancellationToken ct = default) => WriteRegInternalAsync(Hold4xAddr.MaxZ, value, ct);

        public Task Go_Sensor_Home(CancellationToken ct = default) => GoSensorHomeAsync(ct);
        public Task Set_Point(CancellationToken ct = default) => SetPointAsync(ct);
        public Task Emergency(CancellationToken ct = default) => EmergencyAsync(ct);
        public Task Restart(CancellationToken ct = default) => RestartAsync(ct);
        public Task STOP(CancellationToken ct = default) => StopAsync(ct);
        public Task Jog_Stop_All(CancellationToken ct = default) => JogStopAllAsync(ct);

        public Task Write_Coil0x(Coil0xAddr addr, bool value, CancellationToken ct = default) => WriteCoilAsync(addr, value, ct);
        public Task Pulse_Coil0x(Coil0xAddr addr, int pulseMs = DefaultPulseMs, CancellationToken ct = default) => PulseCoilAsync(addr, pulseMs, ct);
        public Task Write_Hold4x(Hold4xAddr addr, ushort value, CancellationToken ct = default) => WriteRegisterAsync(addr, value, ct);

        private void Emit(LogDir dir, string msg, string? detail = null)
        {
            RaiseOnUI(() => LogProduced?.Invoke(new LogItem(DateTime.Now, dir, msg, detail)));
        }

        private void RaiseOnUI(Action action)
        {
            var ctx = _uiCtx;
            if (ctx is null) { action(); return; }
            ctx.Post(_ => action(), null);
        }

        public async ValueTask DisposeAsync()
        {
            try { await DisconnectAsync().ConfigureAwait(false); } catch { }
            _ioGate.Dispose();
        }
        public async Task<ushort> ReadPosYAsync(CancellationToken ct = default)
        {
            var client = _client;
            if (!_connected || client is null) throw new InvalidOperationException("MCU not connected.");

            int timeout = IoTimeoutMs > 0 ? IoTimeoutMs : 800;

            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
          
                var regs = await client.ReadInputRegistersAsync(_unitId, 4, 1, timeout, ct).ConfigureAwait(false);
                return regs[0];
            }
            finally { _ioGate.Release(); }
        }
        public async Task<bool[]> ReadOutputs18Async(CancellationToken ct = default)
        {
            var client = _client;
            if (!_connected || client is null) throw new InvalidOperationException("MCU not connected.");

            const ushort start = 17;  // coil 00017
            const ushort count = 18;  // 18 outputs

            await _ioGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await client.ReadCoilsAsync(_unitId, start, count, IoTimeoutMs, ct)
                                   .ConfigureAwait(false);
            }
            finally { _ioGate.Release(); }
        }
    }
}
