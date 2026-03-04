using Measurement_MC_App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Service
{
    public interface IMcuModbusService: IAsyncDisposable
    {
        bool IsConnected { get; }
        event Action<bool>? ConnectionChanged;

        event Action<LogItem>? LogProduced;
        event Action<McuStatus>? StatusChanged;
        bool EnablePollingLog { get; set; }

        int IdlePollMs { get; set; }
        int MovingPollMs { get; set; }
        int ErrorPollMs { get; set; }
        int IoTimeoutMs { get; set; }
        Task ConnectAsync(string portName, int baudRate, byte unitId, CancellationToken ct = default);
        Task DisconnectAsync(CancellationToken ct = default);

        Task StartListeningAsync(CancellationToken ct = default);
        Task StopListeningAsync(CancellationToken ct = default);
        Task GoSensorHomeAsync(CancellationToken ct = default);
        Task SetPointAsync(CancellationToken ct = default);
        Task EmergencyAsync(CancellationToken ct = default);
        Task RestartAsync(CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);
        Task JogOxMinusDownAsync(CancellationToken ct = default);
        Task JogOxMinusUpAsync(CancellationToken ct = default);

        Task JogOxPlusDownAsync(CancellationToken ct = default);
        Task JogOxPlusUpAsync(CancellationToken ct = default);

        Task JogOyMinusDownAsync(CancellationToken ct = default);
        Task JogOyMinusUpAsync(CancellationToken ct = default);

        Task JogOyPlusDownAsync(CancellationToken ct = default);
        Task JogOyPlusUpAsync(CancellationToken ct = default);

        Task JogOzMinusDownAsync(CancellationToken ct = default);
        Task JogOzMinusUpAsync(CancellationToken ct = default);

        Task JogOzPlusDownAsync(CancellationToken ct = default);
        Task JogOzPlusUpAsync(CancellationToken ct = default);

        Task JogStopAllAsync(CancellationToken ct = default);
        Task Output1Async(bool on, CancellationToken ct = default);
        Task Output2Async(bool on, CancellationToken ct = default);
        Task Output3Async(bool on, CancellationToken ct = default);
        Task Output4Async(bool on, CancellationToken ct = default);
        Task Output5Async(bool on, CancellationToken ct = default);
        Task Output6Async(bool on, CancellationToken ct = default);
        Task Output7Async(bool on, CancellationToken ct = default);
        Task Output8Async(bool on, CancellationToken ct = default);
        Task Output9Async(bool on, CancellationToken ct = default);
        Task Output10Async(bool on, CancellationToken ct = default);
        Task Output11Async(bool on, CancellationToken ct = default);
        Task Output12Async(bool on, CancellationToken ct = default);
        Task Output13Async(bool on, CancellationToken ct = default);
        Task Output14Async(bool on, CancellationToken ct = default);
        Task Output15Async(bool on, CancellationToken ct = default);
        Task Output16Async(bool on, CancellationToken ct = default);
        Task SetTargetXAsync(ushort value, CancellationToken ct = default);
        Task SetSpeedTarXAsync(ushort value, CancellationToken ct = default);
        Task SetMaxXAsync(ushort value, CancellationToken ct = default);

        Task SetTargetYAsync(ushort value, CancellationToken ct = default);
        Task SetSpeedTarYAsync(ushort value, CancellationToken ct = default);
        Task SetMaxYAsync(ushort value, CancellationToken ct = default);

        Task SetTargetZAsync(ushort value, CancellationToken ct = default);
        Task SetSpeedTarZAsync(ushort value, CancellationToken ct = default);
        Task SetMaxZAsync(ushort value, CancellationToken ct = default);
        Task WriteCoilAsync(Coil0xAddr addr, bool value, CancellationToken ct = default);
        Task PulseCoilAsync(Coil0xAddr addr, int pulseMs = 100, CancellationToken ct = default);
        Task WriteRegisterAsync(Hold4xAddr addr, ushort value, CancellationToken ct = default);
        Task<ushort> ReadPosYAsync(CancellationToken ct = default);
        Task<bool[]> ReadOutputs18Async(CancellationToken ct = default);
    }
}
