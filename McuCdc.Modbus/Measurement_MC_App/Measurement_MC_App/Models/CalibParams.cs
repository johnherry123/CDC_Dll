using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public sealed class CalibParams
    {
        public double PixelToMnX { get; set; } = 0.01;
        public double PixelToMnY { get; set; } = 0.01;
    }
}
