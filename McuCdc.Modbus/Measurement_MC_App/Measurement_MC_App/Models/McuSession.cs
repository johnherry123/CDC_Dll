using Measurement_MC_App.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public sealed class McuSession
    {
        public IMcuModbusService Service { get; }

        public bool IsConnected => Service.IsConnected;

        public McuSession(IMcuModbusService service)
        {
            Service = service;
        }

        public Task EnsureConnectedAsync(string port, int baud, byte uid, CancellationToken ct = default)
        {
            if (Service.IsConnected) return Task.CompletedTask;
            return Service.ConnectAsync(port, baud, uid, ct); // connect 1 lần
        }

        public Task DisconnectAsync(CancellationToken ct = default)
            => Service.DisconnectAsync(ct);
    }
}
