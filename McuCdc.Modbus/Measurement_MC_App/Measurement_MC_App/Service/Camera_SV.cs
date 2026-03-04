using Basler.Pylon;
using Measurement_MC_App.Logic;
using Measurement_MC_App.ViewModels;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Windows;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;



namespace Measurement_MC_App.Service
{
    public class Camera_SV
    {
        private static Camera_SV? _instance;
        private static readonly object _lock = new object();
        private int camera_FPS = 30;
        private OperatingMode mode = OperatingMode.Normal;
        private bool _isGrapping = false;
        public static Camera_SV Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Camera_SV();
                        }
                    }
                }
                return _instance;
            }
        }

        public int Camera_FPS { get => camera_FPS; set => camera_FPS = value; }
        public bool IsGrapping { get => _isGrapping; set => _isGrapping = value; }
        public OperatingMode Mode { get => mode; set => mode = value; }

        public event EventHandler<string> Pub;

        private Camera_SV()
        {
            // Private constructor to prevent instantiation
        }
        private string _IP = "192.168.165.189";
        private int _Port = 8000;
        private Camera _camera;
        PixelDataConverter converter = new PixelDataConverter();
        private const int DISPLAY_WIDTH = 800;

        public void Connect(string ipAddress = "")
        {
            try
            {
                _camera = new Camera();   // Lấy camera đầu tiên
                _camera.Open();
                //Create Trackbar window
                //Camera_BLL.Instance.CreateCameraTuningBar();
                // Cấu hình cơ bản
                _camera.Parameters[PLCamera.PixelFormat].SetValue(PLCamera.PixelFormat.Mono8);
                _camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);

                _camera.Parameters[PLCamera.AcquisitionFrameRateEnable].SetValue(true);
                _camera.Parameters[PLCamera.AcquisitionFrameRateAbs].SetValue(Camera_FPS);

                _camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                _camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                //MessageBox.Show("Camera opened OK");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void DisconnectBaslerCamera()
        {
            //if (_camera != null)
            //{
            //    if (_camera.IsGrabbing)
            //        _camera.StopGrabbing();

            //    if (_camera.IsOpen)
            //        _camera.Close();

            //    _camera.Dispose();
            //    _camera = null;
            //}
        }
        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                var r = e.GrabResult;
                if (!r.GrabSucceeded)
                {
                    IsGrapping = false;
                    return;
                }

                Mat mat = new Mat(
                    r.Height,
                    r.Width,
                    MatType.CV_8UC4);

                converter.OutputPixelFormat = PixelType.BGRA8packed;
                converter.Convert(mat.Data, mat.Step() * mat.Rows, r);

                
                Mat safeMat = mat.Clone();

                if(Mode == OperatingMode.Normal)
                {
                    Camera_BLL.Instance.ProcessImage(safeMat);
                }
                else if(Mode == OperatingMode.Calibration)
                {
                    Camera_BLL.Instance.ProcessCalibration(safeMat);
                }

                Pub?.Invoke(this, "FrameReady");
                IsGrapping = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        
        void ProcessAndDisplay(Mat mat)
        {
            (Mat result, Mat edges) = ProcessImage(mat);

            System.Drawing.Bitmap bmpResult = BitmapConverter.ToBitmap(result);
            System.Drawing.Bitmap bmpEdges = BitmapConverter.ToBitmap(edges);

            //BeginInvoke(new Action(() =>
            //{
                
            //}));
        }
        (Mat result, Mat edges) ProcessImage(Mat frame)
        {
            // 1. Resize để đồng bộ
            double scale = (double)DISPLAY_WIDTH / frame.Cols;
            int newHeight = (int)(frame.Rows * scale);

            Mat img = frame.Resize(new Size(DISPLAY_WIDTH, newHeight));

            // Trackbar values (giả sử bạn đã có hàm này)
            (int blurK, int cannyL, int cannyH) = GetTrackbarValues();

            // 2. Tiền xử lý
            Mat gray = new();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

            Mat blur = new();
            Cv2.GaussianBlur(gray, blur, new Size(blurK, blurK), 0);

            Mat edges = new();
            Cv2.Canny(blur, edges, cannyL, cannyH);

            // 3. Find contours – lấy full cây (RETR_TREE)
            Cv2.FindContours(
                edges,
                out Point[][] contours,
                out HierarchyIndex[] hierarchy,
                RetrievalModes.Tree,
                ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
                return (img, edges);

            List<(Point[] cnt, RotatedRect rect, double w, double h)> validContours
                = new();

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
                if (w < 610 && h < 200)
                {
                    validContours.Add((cnt, rect, w, h));
                }
            }

            if (validContours.Count == 0)
            {
                Cv2.PutText(
                    img,
                    "No contour matches size limit",
                    new Point(20, 40),
                    HersheyFonts.HersheySimplex,
                    0.7,
                    Scalar.Red,
                    2);

                return (img, edges);
            }

            // 4. Lấy contour lớn nhất
            var best = validContours
                .OrderByDescending(x => Cv2.ContourArea(x.cnt))
                .First();

            var (bestCnt, bestRect, bw, bh) = best;
            var center = bestRect.Center;

            Mat result = img.Clone();

            Point2f[] boxPts = bestRect.Points();
            Point[] box = Array.ConvertAll(boxPts, p => (Point)p);

            Cv2.DrawContours(result, new[] { box }, -1, Scalar.Red, 2);

            string textPx = $"PX: {(int)bw}x{(int)bh}";
            Cv2.PutText(
                result,
                textPx,
                new Point((int)center.X - 60, (int)center.Y - 10),
                HersheyFonts.HersheySimplex,
                0.6,
                Scalar.Yellow,
                2);

            return (result, edges);
        }

        private (int blurK, int cannyL, int cannyH) GetTrackbarValues()
        {
            return (5 , 25, 25);
        }


    }
}
