using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models.Database
{
    public class Connector
    {
        [Key]
        public int ConnectorId { get; set; }

        [MaxLength(64)]
        public string Host { get; set; } = "192.168.0.10";

        [MaxLength(32)]
        public string PortName { get; set; } = "COM4";

        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;

        [MaxLength(16)]
        public string Parity { get; set; } = "None";

        [MaxLength(16)]
        public string StopBits { get; set; } = "One";

        public int SpeedX { get; set; }
        public int SpeedY { get; set; }
        public int SpeedZ { get; set; }

        public int AxisXMax { get; set; }
        public int AxisYMax { get; set; }
        public int AxisZMax { get; set; }

        public int SafeZ { get; set; }
    }
}
