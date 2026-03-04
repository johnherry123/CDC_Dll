using McuCdc.Modbus.Spooling;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Client
{
    public sealed class ClientOptions
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public bool DtrEnable { get; set; } = false;
        public bool RtsEnable { get; set; } = false;
        public int ReadTimeoutMs { get; set; } = 50;
        public int WriteTimeoutMs { get; set; } = 200;
        public AddressingMode AddressingMode { get; set; } = AddressingMode.AsProvided;
        public int TxQueueCapacity { get; set; } = 256;
        public int RxFrameQueueCapacity { get; set; } = 1024;
        public int ReadBufferSize { get; set; } = 4096;
        public int WatchdogPeriodMs { get; set; } = 500;
        public int StallReadMs { get; set; } = 2500;
        public int ReconnectDelayMs { get; set; } = 300;
        public SpoolOptions Spool { get; set; } = new SpoolOptions { Enabled = false };
    }
}
