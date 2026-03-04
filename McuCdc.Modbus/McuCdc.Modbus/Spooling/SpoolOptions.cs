using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Spooling
{
    public sealed class SpoolOptions
    {
        public bool Enabled { get; set; }

        public string DirectoryPath { get; set; } = "spool/modbus_rx";

        public long MaxSegmentBytes { get; set; } = 32L * 1024 * 1024;

        public int FlushEveryNFrames { get; set; } = 50;
    }
}
