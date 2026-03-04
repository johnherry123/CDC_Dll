using OpenCvSharp.Internal.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Measurement_MC_App.Models;
using Measurement_MC_App.Service;
using System.Diagnostics;

namespace Measurement_MC_App.Logic
{
    public class Handler_BLL
    {
        public Handler_BLL() { }
        private IMcuModbusService MCUservice => McuComHub.Service;
        private McuStatusSnapshot MCUstatus => McuState.Current;
        //IMcuModbusService mcuModbusService = new McuModbusService();
        private double Height_standard = 2.54;
        private double Width_standard = 14.16;
        private double Height_tolerance_p = 0.05;
        private double Height_tolerance_n = 0.05;
        private double Width_tolerance_p = 0.05;
        private double Width_tolerance_n = 0.05;
        public ushort z_Move_max { get; set; } = 5000;
        public event EventHandler<string>? Pub;
        private TrayPoints[] trayPoints_array = new TrayPoints[6];
        private void Wait_Handler_ready()
        {
            int num = 0;
            while ((MCUstatus.StateX & MCUstatus.StateY & MCUstatus.StateZ) ==0 && num < 100)
            {
                Task.Delay(10).Wait();
                num++;
            }
        }
        public void Go_2_target(Point3D Target)
        {
            bool read = false;
            if(read == false/*chage tho read cur_z*/)
            {
                MCUservice.SetTargetYAsync(z_Move_max).Wait();
            }
            Wait_Handler_ready();
            MCUservice.SetTargetXAsync((ushort)(Target.X)).Wait();
            MCUservice.SetTargetYAsync((ushort)(Target.Y)).Wait();
            Wait_Handler_ready();
            MCUservice.SetTargetYAsync((ushort)(Target.Z)).Wait();
            Wait_Handler_ready();

        }
        public void Mark()
        {

        }
        public void SetTrayPoints_array(TrayPoints[] trayPoints_array)
        {
            this.trayPoints_array = trayPoints_array;
            for(int i = 0;i< trayPoints_array.Length;i++)
            {
                Calculate_marker_position(ref trayPoints_array[i]);
            }
        }
        public void Calculate_marker_position(ref TrayPoints tray)
        {
            int rows = TrayPoints.Rows;
            int cols = TrayPoints.Cols;
            double dx_row = (tray.P3.X - tray.P1.X) / (rows - 1);
            double dy_row = (tray.P3.Y - tray.P1.Y) / (rows - 1);
            double dx_col = (tray.P2.X - tray.P1.X) / (cols - 1);
            double dy_col = (tray.P2.Y - tray.P1.Y) / (cols - 1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double x = tray.P1.X + c * dx_col + r * dx_row;
                    double y = tray.P1.Y + c * dy_col + r * dy_row;
                    tray.Point2Ds[r * cols + c] = new Point3D(x, y);
                }
            }
        }
        public bool Measure()
        {
            //double H = Camera_BLL.Instance.Height_real;
            //double W = Camera_BLL.Instance.Witdh_real;
            //if (H <= Height_standard + Height_tolerance_p && H >= Height_standard - Height_tolerance_n &&
            //    W <= Width_standard + Width_tolerance_p && W >= Width_standard - Width_tolerance_n)
            //{
            //    return true;
            //}
            //return false;
            Debug.WriteLine(string.Format("Height = {0}, Width = {1}", Camera_BLL.Instance.Height_real, Camera_BLL.Instance.Witdh_real));
            if (Camera_BLL.Instance.Height_real < 2.54 && Camera_BLL.Instance.Witdh_real < 14.20)
            {
                
                return true;
            }    
            return false;
        }
        public void Handler_Run()
        {
            //if(Measure())
            //{
            //    //Pub?.Invoke(this, "Measurement OK");
            //    Debug.WriteLine("Measurement OK");
            //}
            //else
            //{
            //    Debug.WriteLine("Measurement NG");
            //    //Pub?.Invoke(this, "Measurement NG");
            //}
            //for (int i = 0; i < trayPoints_array.Length; i++)
            //{
            //    foreach (Point3D point in trayPoints_array[i].Point2Ds)
            //    {
            //        Go_2_target(point);
            //        int num = 0;
            //        while (!Camera_SV.Instance.IsGrapping && num < 100)
            //        {
            //            Task.Delay(10).Wait();
            //            num++;
            //        }
            //        if (num >= 100)
            //        {
            //            Pub?.Invoke(this, "Camera timeout");
            //            return;
            //        }

            //        if (Measure())
            //        {
            //            point.Status = Point_status.OK;
            //        }
            //        else
            //        {
            //            point.Status = Point_status.NG_size;
            //        }

            //    }
            //}
            MCUservice.SetTargetYAsync(z_Move_max).Wait();
        }
        public void Handler_Mark()
        {
            for (int i = 0; i < trayPoints_array.Length; i++)
            {
                foreach (Point3D point in trayPoints_array[i].Point2Ds)
                {
                    if(point.Status == Point_status.NG_size || point.Status == Point_status.NG_stain)
                    {
                        Go_2_target(point);
                        Mark();
                    }

                }
            }
        }

        public void Handler_Home()
        {
            MCUservice.GoSensorHomeAsync().Wait();
            Wait_Handler_ready();
        }

        public void Emergency_Stop()
        {
            
        }

        public void Reset_Handler()
        {

        }

    }
}
