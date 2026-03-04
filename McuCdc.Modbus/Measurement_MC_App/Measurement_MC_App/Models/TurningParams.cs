using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public sealed class TurningParams
    {
        public int Blur { get; set; } = 5;
        public int CannyLow { get; set; } = 60;
        public int CannyHigh { get; set; } = 120;
    }
}
