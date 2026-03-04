using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public sealed class TrayPoints
    {
        public int TrayIndex { get; set; } 
        public static int Rows { get; set; } = 14;
        public static int Cols { get; set; } = 15;
        public Point3D P1 { get; set; } = new(0, 0);
        public Point3D P2 { get; set; } = new(0, 0);
        public Point3D P3 { get; set; } = new(0, 0);
        public Point3D[] Point2Ds { get; set; } = new Point3D[Rows * Cols];
        public TrayPoints()
        {

        }
    }
}
