using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public enum Point_status
    {
        None,
        OK,
        NG_size,
        NG_stain
    }
    public sealed class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Point3D() { }
        public Point_status Status { get; set; } = Point_status.None;
        public Point3D(double x, double y)
        {
            X = x;
            Y = y;
        }
        public override string ToString() => $"{X:0.##}, {Y:0.##}";
    }
}
