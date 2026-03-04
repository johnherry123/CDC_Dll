using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Measurement_MC_App.Helps;
using Measurement_MC_App.Logic;
using Measurement_MC_App.Models;
using Measurement_MC_App.Service;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Measurement_MC_App.ViewModels
{
    public enum MotorMode { Tap, Manual }

    public partial class MotorViewModel : ObservableObject, IDisposable
    {
        public const double RangeX = 550.0;
        public const double RangeY = 280.0;
        public const double RangeZ = 130.0;
        private const double RawToMm = 0.01;
        private const double MmToRaw = 100.0;
        private IMcuModbusService Service => McuComHub.Service;
        private readonly DispatcherTimer _pollTimer;
        private int _pollingFlag;
        [ObservableProperty] private BitmapSource? cameraImage;
        [ObservableProperty] private string cameraStatus = "Idle";
        public ComSettings com = App.Host.Services.GetRequiredService<ComSettings>();

        [ObservableProperty] private bool isIoOutput = true;
        public bool IsIoInput => !IsIoOutput;
        partial void OnIsIoOutputChanged(bool value) => OnPropertyChanged(nameof(IsIoInput));

        public ObservableCollection<IoPoint> Outputs { get; } = new();
        public ObservableCollection<IoPoint> Inputs { get; } = new();
        [ObservableProperty] private MotorMode mode = MotorMode.Tap;
        private const int IoQty = 18;
        private const ushort OutputDisplayStart = 17;     
        private const ushort InputDisplayStart = 10001;
        public bool IsTap => Mode == MotorMode.Tap;
        public bool IsManual => Mode == MotorMode.Manual;

        partial void OnModeChanged(MotorMode value)
        {
            OnPropertyChanged(nameof(IsTap));
            OnPropertyChanged(nameof(IsManual));
        }

        [ObservableProperty] private double currentX; 
        [ObservableProperty] private double currentY; 
        [ObservableProperty] private double currentZ; 
        [ObservableProperty] private double targetX; 
        [ObservableProperty] private double targetY;
        [ObservableProperty] private double targetZ;

        public double CurrentXmm => CurrentX * RawToMm;
        public double CurrentYmm => CurrentY * RawToMm;
        public double CurrentZmm => CurrentZ * RawToMm;

        partial void OnCurrentXChanged(double value)
        {
            OnPropertyChanged(nameof(CurrentXmm));
            UpdateMarkerFromCurrent();
        }
        partial void OnCurrentYChanged(double value)
        {
            OnPropertyChanged(nameof(CurrentYmm));
            UpdateMarkerFromCurrent();
        }
        partial void OnCurrentZChanged(double value) => OnPropertyChanged(nameof(CurrentZmm));
        [ObservableProperty] private double markerLeft = 18;
        [ObservableProperty] private double markerTop = 18;

        private double _mapW = 1;
        private double _mapH = 1;
        [ObservableProperty] private bool isXPlusPressed;
        [ObservableProperty] private bool isXMinusPressed;
        [ObservableProperty] private bool isYPlusPressed;
        [ObservableProperty] private bool isYMinusPressed;
        [ObservableProperty] private bool isZPlusPressed;
        [ObservableProperty] private bool isZMinusPressed;

        private string? _activeDir;
        public ObservableCollection<TrayPoints> Trays { get; } = new();
        [ObservableProperty] private int selectedTrayIndex = 1;

        public TrayPoints? SelectedTray
        {
            get
            {
                if (Trays.Count == 0) return null;
                var idx = Math.Clamp(SelectedTrayIndex - 1, 0, Trays.Count - 1);
                return Trays[idx];
            }
        }

        partial void OnSelectedTrayIndexChanged(int value) => OnPropertyChanged(nameof(SelectedTray));
        [ObservableProperty] private string statusText = "Ready";
        private readonly Dictionary<string, Func<Task>> _jogDown;
        private readonly Dictionary<string, Func<Task>> _jogUp;
        private bool _disposed;

        public MotorViewModel()
        {
            for (int i = 0; i < 6; i++)
                Trays.Add(new TrayPoints());
            for (int i = 1; i <= 18; i++)
            {
                Outputs.Add(new IoPoint($"DO{i:00}", $"Output {i:00}", isOn: false));
                Inputs.Add(new IoPoint($"DI{i:00}", $"Input {i:00}", isOn: false));
            }
            _jogDown = new()
            {
                ["X+"] = () => Service.JogOxPlusDownAsync(),
                ["X-"] = () => Service.JogOxMinusDownAsync(),
                ["Y+"] = () => Service.JogOyPlusDownAsync(),
                ["Y-"] = () => Service.JogOyMinusDownAsync(),
                ["Z+"] = () => Service.JogOzPlusDownAsync(),
                ["Z-"] = () => Service.JogOzMinusDownAsync(),
            };

            _jogUp = new()
            {
                ["X+"] = () => Service.JogOxPlusUpAsync(),
                ["X-"] = () => Service.JogOxMinusUpAsync(),
                ["Y+"] = () => Service.JogOyPlusUpAsync(),
                ["Y-"] = () => Service.JogOyMinusUpAsync(),
                ["Z+"] = () => Service.JogOzPlusUpAsync(),
                ["Z-"] = () => Service.JogOzMinusUpAsync(),
            };

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _pollTimer.Tick += PollTimer_Tick;

            McuComHub.Service.StatusChanged += OnMcuStatusChanged;

            _ = InitializeAsync();
        }
        private async Task InitializeAsync()
        {
            
            try
            {
                StatusText = "Connecting...";
                Camera_SV.Instance.Pub -= OnCameraPub;
                Camera_SV.Instance.Pub += OnCameraPub;

                _pollTimer.Start();
                StatusText = "Connected. Polling started.";
            }
            catch (Exception ex)
            {
                StatusText = $"Init fail: {ex.Message}";
            }
        }

        private void OnMcuStatusChanged(McuStatus st)
        {
            McuState.UpdateFrom(st);
        }

        private void OnCameraPub(object? sender, string e)
        {
            try
            {
                CameraImage = Convert_Image_Helper.BitmapToBitmapSource(Camera_BLL.Instance.Face);
            }
            catch (Exception ex)
            {
                StatusText = $"Camera frame error: {ex.Message}";
            }
        }
        private async void PollTimer_Tick(object? sender, EventArgs e)
        {
            if (com.IsConnected == true)
            {
                await PollUiAsync();
            }

        }

        private async Task PollUiAsync()
        {
            if (Interlocked.Exchange(ref _pollingFlag, 1) == 1) return;

            try
            {
   
                CurrentX = McuState.Current.PosX;
                CurrentY = McuState.Current.PosY;
                CurrentZ = McuState.Current.PosZ;

             
                ApplyIoFromState();

                StatusText = $"Updated {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText = $"Poll error: {ex.Message}";
            }
            finally
            {
                Interlocked.Exchange(ref _pollingFlag, 0);
            }

            await Task.CompletedTask;
        }

        private void ApplyIoFromState()
        {
            for (int i = 0; i < 18 && i < Outputs.Count; i++)
            {
                ushort coilAddr = (ushort)(i+17);
                Outputs[i].IsOn = McuState.Current.Coil0x[coilAddr];
            }   
            for (int i = 0; i < 18 && i < Inputs.Count; i++)
            {
                int idx = 1 + i; 
                Inputs[i].IsOn = McuState.Current.In1x[idx];
            }
        }

        public void SetMapSize(double w, double h)
        {
            _mapW = Math.Max(1, w);
            _mapH = Math.Max(1, h);
            UpdateMarkerFromCurrent();
        }

        private void UpdateMarkerFromCurrent()
        {
            var xMm = Math.Clamp(CurrentX * RawToMm, 0, RangeX);
            var yMm = Math.Clamp(CurrentY * RawToMm, 0, RangeY);

            var px = (xMm / RangeX) * _mapW;
            var py = (yMm / RangeY) * _mapH;

            MarkerLeft = px - 9;
            MarkerTop = py - 9;
        }

        [RelayCommand] private void StartCamera() => CameraStatus = "Running";
        [RelayCommand] private void StopCamera() => CameraStatus = "Stopped";

        [RelayCommand] private void ShowIoOutput() => IsIoOutput = true;
        [RelayCommand] private void ShowIoInput() => IsIoOutput = false;

        [RelayCommand] private void SetModeTap() => Mode = MotorMode.Tap;
        [RelayCommand] private void SetModeManual() => Mode = MotorMode.Manual;

        [RelayCommand]
        private async Task Home()
        {
            try
            {
                await Service.GoSensorHomeAsync();
                StatusText = "HOME sent";
            }
            catch (Exception ex)
            {
                StatusText = $"HOME error: {ex.Message}";
            }
        }

        public async Task SetTargetFromTapAsync(double xMm, double yMm, double markerPxX, double markerPxY)
        {
            try
            {
                xMm = Math.Clamp(xMm, 0, RangeX);
                yMm = Math.Clamp(yMm, 0, RangeY);

                TargetX = xMm;
                TargetY = yMm;

                ushort x = ToU16(xMm * MmToRaw);
                ushort y = ToU16(yMm * MmToRaw);

                await Service.SetTargetXAsync(x);
                await Service.SetTargetYAsync(y);
                await Service.SetPointAsync();

                StatusText = $"Tap -> X={xMm:0.##} Y={yMm:0.##}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }

   
        }

      
        public async void SetTargetFromTap(double xMm, double yMm, double markerPxX, double markerPxY)
            => await SetTargetFromTapAsync(xMm, yMm, markerPxX, markerPxY);

   
        [RelayCommand]
        private async Task ApplyManual()
        {
            try
            {
                TargetX = Math.Clamp(TargetX, 0, RangeX);
                TargetY = Math.Clamp(TargetY, 0, RangeY);
                TargetZ = Math.Clamp(TargetZ, 0, RangeZ);

                ushort x = ToU16(TargetX * MmToRaw);
                ushort y = ToU16(TargetY * MmToRaw);
                ushort z = ToU16(TargetZ * MmToRaw);

                await Service.SetTargetXAsync(x);
                await Service.SetTargetYAsync(y);
                await Service.SetTargetZAsync(z);
                await Service.SetPointAsync();

                StatusText = $"Apply -> X={TargetX:0.##} Y={TargetY:0.##} Z={TargetZ:0.##}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
        }

        private static ushort ToU16(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return 0;
            if (v <= 0) return 0;
            if (v >= ushort.MaxValue) return ushort.MaxValue;
            return (ushort)Math.Round(v);
        }
        public void OnAxisButtonDown(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            _activeDir = dir;

            SetPressState(dir, true);

            if (_jogDown.TryGetValue(dir, out var fn))
            {
                SafeFireAndForget(fn, $"JOG DOWN {dir}");
                StatusText = $"DOWN {dir}";
            }
        }

        public void OnAxisButtonUp(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;

            SetPressState(dir, false);

            if (_jogUp.TryGetValue(dir, out var fn))
            {
                SafeFireAndForget(fn, $"JOG UP {dir}");
                StatusText = $"UP {dir}";
            }

            if (string.Equals(_activeDir, dir, StringComparison.OrdinalIgnoreCase))
                _activeDir = null;
        }

        public void SetPressState(string dir, bool isOn)
        {
            switch (dir)
            {
                case "X+": IsXPlusPressed = isOn; break;
                case "X-": IsXMinusPressed = isOn; break;
                case "Y+": IsYPlusPressed = isOn; break;
                case "Y-": IsYMinusPressed = isOn; break;
                case "Z+": IsZPlusPressed = isOn; break;
                case "Z-": IsZMinusPressed = isOn; break;
            }
        }

        public void StopAllPress()
        {
            IsXPlusPressed = false;
            IsXMinusPressed = false;
            IsYPlusPressed = false;
            IsYMinusPressed = false;
            IsZPlusPressed = false;
            IsZMinusPressed = false;

            if (!string.IsNullOrWhiteSpace(_activeDir))
            {
                var d = _activeDir;
                _activeDir = null;

                if (_jogUp.TryGetValue(d, out var fn))
                    SafeFireAndForget(fn, $"JOG UP {d} (StopAll)");
            }
        }

        private void SafeFireAndForget(Func<Task> action, string context)
        {
            _ = Task.Run(async () =>
            {
                try { await action().ConfigureAwait(false); }
                catch (Exception ex)
                {
                    // UI update must be on dispatcher
                    Application.Current?.Dispatcher?.Invoke(() =>
                        StatusText = $"{context} error: {ex.Message}");
                }
            });
        }

        public void OnHandlerDown(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            StatusText = $"HANDLER DOWN {tag}";
        }

        public void OnHandlerUp(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            StatusText = $"HANDLER UP {tag}";
        }

        [RelayCommand] private void Pick() { /* TODO implement */ }
        [RelayCommand] private void Release() { /* TODO implement */ }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _pollTimer.Stop(); } catch { }
            try { _pollTimer.Tick -= PollTimer_Tick; } catch { }

            try { McuComHub.Service.StatusChanged -= OnMcuStatusChanged; } catch { }
            try { Camera_SV.Instance.Pub -= OnCameraPub; } catch { }
        }
    }
}
