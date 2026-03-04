using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models.Database
{
    public class VisionParam
    {
        [Key, ForeignKey(nameof(Model))]
        public int ModelId { get; set; }

        public double Blur { get; set; } = 13;
        public double CannyLow { get; set; } = 71;
        public double CannyHigh { get; set; } = 158;

        public double RefObjectWidth { get; set; } = 14.141;
        public double RefObjectHeight { get; set; } = 2.462;

        public double MmPerPixelWidth { get; set; } = 0;
        public double MmPerPixelHeight { get; set; } = 0;

        public double RealObjectWidth { get; set; } = 0;
        public double RealObjectHeight { get; set; } = 0;

        public Model? Model { get; set; }
    }
}
