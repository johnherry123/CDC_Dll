using Measurement_MC_App.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public static class McuComHub
    {
        public static IMcuModbusService Service { get; } = new McuModbusService();

        private static readonly SemaphoreSlim _gate = new(1, 1);
        public static string PortName { get; private set; } = "COM1";
        public static int BaudRate { get; private set; } = 115200;
        public static byte UnitId { get; private set; } = 1;
        public static bool IsConnected => Service.IsConnected;

        public static event Action<bool>? ConnectionChanged
        {
            add => Service.ConnectionChanged += value;
            remove => Service.ConnectionChanged -= value;
        }
        public static event Action<string>? LogText;
        public static void Configure(string portName, int baudRate, byte unitId)
        {
            PortName = portName;
            BaudRate = baudRate;
            UnitId = unitId;
        }
        public static async Task EnsureConnectedAsync(CancellationToken ct = default)
        {
            if (Service.IsConnected) return;

            await _gate.WaitAsync(ct);
            try
            {
                if (Service.IsConnected) return;

                LogText?.Invoke($"Connecting {PortName} @ {BaudRate}, UID={UnitId}...");
                await Service.ConnectAsync(PortName, BaudRate, UnitId, ct);
                LogText?.Invoke("Connected.");
            }
            finally
            {
                _gate.Release();
            }
        }
        public static async Task DisconnectAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (!Service.IsConnected) return;

                LogText?.Invoke("Disconnecting...");
                await Service.DisconnectAsync(ct);
                LogText?.Invoke("Disconnected.");
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
