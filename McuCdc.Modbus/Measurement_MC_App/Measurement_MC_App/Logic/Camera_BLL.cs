using Measurement_MC_App.Service;
using Measurement_MC_App.ViewModels;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Point = OpenCvSharp.Point;

namespace Measurement_MC_App.Logic
{
    public class Camera_BLL
    {
        private static Camera_BLL? _instance;
        private static readonly object _lock = new object();
        public event EventHandler<string> LogicHandler;
        // ---------- User-tunable params ----------
        private int _blur = 1;
        private int _cannyL = 204;
        private int _cannyH = 204;

        private Mat _processedImage = new Mat();
        private bool _isCalibrated = false;
        // ---------- Image / result ----------
        private Bitmap _bmp;
        private Bitmap _edge;
        private Bitmap _face;

        //Calibration parameters
        private double _width_ref;
        private double _height_ref;
        private double _widthmmperpixel;
        private double _heightmmperpixel;

        // Measurement parameters
        private double _width_pixel;
        private double _height_pixel;
        private double _witdh_real ;
        private double _height_real;

        // Calibration smoothing
        private double _widthSmoothCalib = 0;
        private double _heightSmoothCalib = 0;

        // Measurement smoothing
        private double _widthSmoothMeas = 0;
        private double _heightSmoothMeas = 0;

        private const float ALPHA = 0.3f;

        public static Camera_BLL Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Camera_BLL();
                        }
                    }
                }
                return _instance;
            }
        }

        public int Blur { get => _blur; set => _blur = value; } // save
        public int CannyL { get => _cannyL; set => _cannyL = value; }// save
        public int CannyH { get => _cannyH; set => _cannyH = value; }//save
        public Bitmap Bmp { get => _bmp; set => _bmp = value; }
        public Bitmap Edge { get => _edge; set => _edge = value; }
        public Bitmap Face { get => _face; set => _face = value; }
        public double Width_ref { get => _width_ref; set => _width_ref = value; }   //save
        public double Height_ref { get => _height_ref; set => _height_ref = value; }    //save
        public double Widthmmperpixel { get => _widthmmperpixel; set => _widthmmperpixel = value; } // save
        public double Heightmmperpixel { get => _heightmmperpixel; set => _heightmmperpixel = value; }  //save
        public double Width_pixel { get => _width_pixel; private set => _width_pixel = value; }
        public double Height_pixel { get => _height_pixel; private set => _height_pixel = value; }
        public double Witdh_real { get => _witdh_real; private set => _witdh_real = value; }
        public double Height_real { get => _height_real; private set => _height_real = value; }
        //public OperatingMode Mode { get => mode; set => mode = value; }

        private Camera_BLL()
        {
            // Private constructor to prevent instantiation
        }

        public void Center_PointImage(Mat Frame)
        {
            int cx = Frame.Width / 2;
            int cy = Frame.Height / 2;
            int size = 15; // độ dài mỗi nét X

            Cv2.Line(Frame, new Point(cx - size, cy - size), new Point(cx + size, cy + size), Scalar.LightGreen, 2);
            Cv2.Line(Frame, new Point(cx - size, cy + size), new Point(cx + size, cy - size), Scalar.LightGreen, 2);
            //Cv2.Circle(Frame, new Point(cx, cy), 5, Scalar.Red, -1);
        }
        public void Center_PointObject(Mat Frame, Point[] center)
        {
            var m = Cv2.Moments(center);
            if (m.M00 != 0)
            {
                int cx = (int)(m.M10 / m.M00);
                int cy = (int)(m.M01 / m.M00);
                Cv2.Circle(Frame, new Point(cx, cy), 5, Scalar.Blue, -1);
            }
        }
        private void SmoothCalibration(double w, double h)
        {
            _widthSmoothCalib = ALPHA * w + (1 - ALPHA) * _widthSmoothCalib;
            _heightSmoothCalib = ALPHA * h + (1 - ALPHA) * _heightSmoothCalib;
        }

        private void SmoothMeasurement(double w, double h)
        {
            _widthSmoothMeas = ALPHA * w + (1 - ALPHA) * _widthSmoothMeas;
            _heightSmoothMeas = ALPHA * h + (1 - ALPHA) * _heightSmoothMeas;
        }

        public void ProcessCalibration(Mat Frame)
        {
            //if(Frame == null)
            //{
            //     LogicHandler?.Invoke(this, "Frame of ProccessCalibration error");
            //}
            //Blur = (Blur % 2 == 0) ? Blur + 1 : Blur;
            Center_PointImage(Frame);
            Mat blur = new Mat();
            Cv2.GaussianBlur(Frame, blur, new OpenCvSharp.Size(Blur, Blur), 0);

            Mat edges = new Mat();
            Cv2.Canny(blur, edges, CannyL, CannyH);


            Cv2.FindContours(
                edges,
                out Point[][] contours,
                out HierarchyIndex[] hierarchy,
                RetrievalModes.Tree,
                ContourApproximationModes.ApproxSimple);

            Mat result = Frame.Clone();
            if (contours.Length == 0)
            {
                Cv2.PutText(result,"Object NOT found",new Point(20, 40),HersheyFonts.HersheySimplex,0.7,Scalar.Red, 2);
                Face = BitmapConverter.ToBitmap(Frame);
                Edge = BitmapConverter.ToBitmap(edges);
                return;
            }
            Point[] bestCnt = null;
            RotatedRect bestRect = new RotatedRect();
            double best_area = 0;

            foreach (var cnt in contours)
            {
                double area = Cv2.ContourArea(cnt);
                if (area < 100)
                    continue;

                RotatedRect rect = Cv2.MinAreaRect(cnt);
                double w = rect.Size.Width;
                double h = rect.Size.Height;

                if (w < h)
                    (w, h) = (h, w);

                // Điều kiện kích thước (giữ nguyên logic bạn)
                //if ((w < 665 && h < 120) && (w > 659))
                if ((w < 668 && h < 120) && (w > 659))
                {
                    if (area > best_area)
                    {
                        best_area = area;
                        bestCnt = cnt;
                        bestRect = rect;
                    }
                }
            }

            if (bestCnt == null)
            {
                Cv2.PutText(result,
                    "No contour matches pixel size",
                    new Point(20, 40),
                    HersheyFonts.HersheySimplex,
                    0.7,
                    Scalar.Red,
                    2);

                Face = BitmapConverter.ToBitmap(result);
                Edge = BitmapConverter.ToBitmap(edges);
                return;
            }
            //Draw rotated rectangle
            //Point2f[] boxPts = bestRect.Points();
            //Point[] box = Array.ConvertAll(boxPts, p => (Point)p);
            //Cv2.DrawContours(result, new[] { box }, -1, Scalar.Red, 2);
            Center_PointObject(result, bestCnt);

            // Draw contour lớn nhất
            Cv2.DrawContours(result, new[] { bestCnt }, -1, Scalar.Red, 2);
            RotatedRect rect2 = Cv2.MinAreaRect(bestCnt);
            float w2 = rect2.Size.Width;
            float h2 = rect2.Size.Height;

            float width_px = Math.Max(w2, h2);
            float height_px = Math.Min(w2, h2);
            SmoothCalibration(width_px, height_px);

            // Update scale calibration
            if (_widthSmoothCalib > 0 && _heightSmoothCalib > 0)
            {
                Widthmmperpixel = Width_ref / _widthSmoothCalib;
                Heightmmperpixel = Height_ref / _heightSmoothCalib;
                _isCalibrated = true;
            }


            Cv2.PutText(result,
                $"mm_per_pixel_w = {Widthmmperpixel:F6}",
                new Point(0, 400),
                HersheyFonts.HersheySimplex,
                0.7,
                Scalar.Red,
                2);
            Cv2.PutText(result,
                $"mm_per_pixel_h = {Heightmmperpixel:F6}",
                new Point(0, 430),
                HersheyFonts.HersheySimplex,
                0.7,
                Scalar.Red,
                2);
            Cv2.PutText(result,
                $"W px = {_widthSmoothCalib:F1} | H px = {_heightSmoothCalib:F1}",
                new Point(0, 460),
                HersheyFonts.HersheySimplex,
                0.7,
                Scalar.Black,
                2);

            Cv2.PutText(result,
                $"Reference Obj: {Width_ref:F3} x {Height_ref:F3} mm",
                new Point(0, 490),
                HersheyFonts.HersheySimplex,
                0.7,
                Scalar.Black,
                2);
          
            //_bmp = BitmapConverter.ToBitmap(result);
            Face = BitmapConverter.ToBitmap(result);
            Edge = BitmapConverter.ToBitmap(edges);

        }
        public void ProcessImage(Mat Frame)
        {
            //if (Frame == null)
            //{
            //    LogicHandler?.Invoke(this, "Frame of ProcessImage error");
            //}
            //Cv2.Resize(Frame, Frame, new OpenCvSharp.Size(500, 500));
            //Blur = (Blur % 2 == 0) ? Blur + 1 : Blur;
            Center_PointImage(Frame);
            Mat blur = new Mat();
            Cv2.GaussianBlur(Frame, blur, new OpenCvSharp.Size(Blur, Blur), 0);

            Mat edges = new Mat();
            Cv2.Canny(blur, edges, CannyL, CannyH);

            Cv2.FindContours(
                edges,
                out Point[][] contours,
                out HierarchyIndex[] hierarchy,
                RetrievalModes.Tree,
                ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                Face = BitmapConverter.ToBitmap(Frame);
                Edge = BitmapConverter.ToBitmap(edges);
                return;
            }

            Mat result = Frame.Clone();

            Point[] bestCnt = null;
            RotatedRect bestRect = new RotatedRect();
            double best_area = 0;

            foreach (var cnt in contours)
            {
                double area = Cv2.ContourArea(cnt);
                if (area < 100)
                    continue;

                RotatedRect rect = Cv2.MinAreaRect(cnt);
                double w = rect.Size.Width;
                double h = rect.Size.Height;

                if (w < h)
                    (w, h) = (h, w);

                // Điều kiện kích thước 
                //if ((w < 665 && h < 120) &&(w > 659))
                if ((w < 668 && h < 120) && (w > 659))
                {
                    if (area > best_area)
                    {
                        best_area = area;
                        bestCnt = cnt;
                        bestRect = rect;
                    }
                }
            }

            if (bestCnt == null)
            {
                Cv2.PutText(
                    result,
                    "No contour matches size limit",
                    new Point(20, 40),
                    HersheyFonts.HersheySimplex,
                    0.7,
                    Scalar.Red,
                    2);

                Face = BitmapConverter.ToBitmap(result);
                Edge = BitmapConverter.ToBitmap(edges);
                return;
            }
            Center_PointObject(result, bestCnt);
            // Draw contour lớn nhất
            Cv2.DrawContours(result, new[] { bestCnt }, -1, Scalar.Red, 2);

            float w2 = bestRect.Size.Width;
            float h2 = bestRect.Size.Height;

            Width_pixel = Math.Max(w2, h2);
            Height_pixel = Math.Min(w2, h2);
            SmoothMeasurement(Width_pixel, Height_pixel);
            if (_widthSmoothMeas != 0 && _heightSmoothMeas != 0)
            {
                if (_isCalibrated)
                {
                    Witdh_real = _widthSmoothMeas * Widthmmperpixel;
                    Height_real = _heightSmoothMeas * Heightmmperpixel;
                }

                Cv2.PutText(result,
                    $"W px = {_widthSmoothMeas:F0} | H px = {_heightSmoothMeas:F0}",
                    new Point(0, 490),
                    HersheyFonts.HersheySimplex,
                    0.7,
                    Scalar.Blue,
                    2);

                Cv2.PutText(result,
                    $"REAL: {Witdh_real:F3} x {Height_real:F3} mm",
                    new Point(0, 460),
                    HersheyFonts.HersheySimplex,
                    0.8,
                    Scalar.Red,
                    2);
            }
            else
            {
                Cv2.PutText(result,
                    "NOT CALIBRATED!",
                    new Point(20, 40),
                    HersheyFonts.HersheySimplex,
                    0.9,
                    Scalar.Red,
                    2);
            }

            //_bmp = BitmapConverter.ToBitmap(result);
            Face = BitmapConverter.ToBitmap(result);
            Edge = BitmapConverter.ToBitmap(edges);

        }
    }
}
