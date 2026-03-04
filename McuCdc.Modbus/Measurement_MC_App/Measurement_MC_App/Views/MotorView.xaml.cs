using Measurement_MC_App.Models;
using Measurement_MC_App.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Measurement_MC_App.Views
{
    public partial class MotorView : UserControl
    {
        public MotorView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;


            MouseLeave += (_, __) => SafeStop();

   
            Application.Current.Deactivated += App_Deactivated;
        }

        private MotorViewModel? VM => DataContext as MotorViewModel;


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
  
            VM?.SetMapSize(MapCanvas.ActualWidth, MapCanvas.ActualHeight);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            SafeStop();
            Application.Current.Deactivated -= App_Deactivated;
        }

        private void App_Deactivated(object? sender, EventArgs e)
        {
            SafeStop();
        }

        private void SafeStop()
        {
            VM?.StopAllPress();


            try
            {
                if (Mouse.Captured is UIElement) Mouse.Capture(null);
            }
            catch { }
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VM is not MotorViewModel vm) return;
            if (sender is not Canvas canvas) return;

            var p = e.GetPosition(canvas);

            var w = canvas.ActualWidth;
            var h = canvas.ActualHeight;
            if (w <= 1 || h <= 1) return;

            var x = p.X / w * MotorViewModel.RangeX;
            var y = p.Y / h * MotorViewModel.RangeY;

            vm.SetTargetFromTap(x, y, p.X, p.Y);
            e.Handled = true;
        }

        private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VM?.SetMapSize(MapCanvas.ActualWidth, MapCanvas.ActualHeight);
        }

        private void QuickBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VM is not MotorViewModel vm) return;
            if (sender is not FrameworkElement bar) return;

            var p = e.GetPosition(bar);

            var bw = bar.ActualWidth;
            var bh = bar.ActualHeight;
            if (bw <= 1 || bh <= 1) return;

            var xMm = p.X / bw * MotorViewModel.RangeX;
            var yMm = p.Y / bh * MotorViewModel.RangeY;

            MoveMarkerToXY(vm, xMm, yMm);
            e.Handled = true;
        }

        private void MoveMarkerToXY(MotorViewModel vm, double xMm, double yMm)
        {
            var w = MapCanvas.ActualWidth;
            var h = MapCanvas.ActualHeight;
            if (w <= 1 || h <= 1) return;

            xMm = Math.Clamp(xMm, 0, MotorViewModel.RangeX);
            yMm = Math.Clamp(yMm, 0, MotorViewModel.RangeY);

            var px = xMm / MotorViewModel.RangeX * w;
            var py = yMm / MotorViewModel.RangeY * h;

            vm.SetTargetFromTap(xMm, yMm, px, py);
        }

        private void TapBtn_Down(object sender, MouseButtonEventArgs e) => BeginHold(sender, e, HoldKind.Jog);
        private void TapBtn_Up(object sender, MouseButtonEventArgs e) => EndHold(sender, e, HoldKind.Jog);
        private void TapBtn_LostCapture(object sender, MouseEventArgs e) => SafeStop();

        private void TapBtn_TouchDown(object sender, TouchEventArgs e) => BeginHold(sender, e, HoldKind.Jog);
        private void TapBtn_TouchUp(object sender, TouchEventArgs e) => EndHold(sender, e, HoldKind.Jog);
        private void TapBtn_LostTouchCapture(object sender, TouchEventArgs e) => SafeStop();

        private void HandlerBtn_Down(object sender, MouseButtonEventArgs e) => BeginHold(sender, e, HoldKind.Handler);
        private void HandlerBtn_Up(object sender, MouseButtonEventArgs e) => EndHold(sender, e, HoldKind.Handler);
        private void HandlerBtn_LostCapture(object sender, MouseEventArgs e) => SafeStop();

        private void HandlerBtn_TouchDown(object sender, TouchEventArgs e) => BeginHold(sender, e, HoldKind.Handler);
        private void HandlerBtn_TouchUp(object sender, TouchEventArgs e) => EndHold(sender, e, HoldKind.Handler);
        private void HandlerBtn_LostTouchCapture(object sender, TouchEventArgs e) => SafeStop();

        private enum HoldKind { Jog, Handler }


        private void BeginHold(object sender, InputEventArgs e, HoldKind kind)
        {
            if (VM is not MotorViewModel vm) return;
            if (sender is not Button btn) return;

            var tag = btn.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(tag)) return;

            if (e is TouchEventArgs te)
            {
                btn.CaptureTouch(te.TouchDevice);
                te.Handled = true;
            }
            else if (e is MouseButtonEventArgs me)
            {
                btn.CaptureMouse();
                me.Handled = true;
            }

            if (kind == HoldKind.Jog)
            {
                vm.SetPressState(tag, true);
                vm.OnAxisButtonDown(tag);
                return;
            }

            ExecuteHandler(vm, tag);
        }

  
        private void EndHold(object sender, InputEventArgs e, HoldKind kind)
        {
            if (VM is not MotorViewModel vm) return;
            if (sender is not Button btn) return;

            var tag = btn.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(tag)) return;


            if (e is TouchEventArgs te)
            {
                try { btn.ReleaseTouchCapture(te.TouchDevice); } catch { }
                te.Handled = true;
            }
            else if (e is MouseButtonEventArgs me)
            {
                if (btn.IsMouseCaptured)
                {
                    try { btn.ReleaseMouseCapture(); } catch { }
                }
                me.Handled = true;
            }

            if (kind == HoldKind.Jog)
            {
                vm.SetPressState(tag, false);
                vm.OnAxisButtonUp(tag);
                return;
            }

            vm.StopAllPress();
        }

        private static void ExecuteHandler(MotorViewModel vm, string tag)
        {
            if (tag.Equals("PICK", StringComparison.OrdinalIgnoreCase))
            {
                if (vm.PickCommand.CanExecute(null)) vm.PickCommand.Execute(null);
                vm.StatusText = "HANDLER: PICK";
                return;
            }

            if (tag.Equals("RELEASE", StringComparison.OrdinalIgnoreCase))
            {
                if (vm.ReleaseCommand.CanExecute(null)) vm.ReleaseCommand.Execute(null);
                vm.StatusText = "HANDLER: RELEASE";
                return;
            }
        }

 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var st = McuState.Current;
            MessageBox.Show(
                $"UpdatedAt={st.UpdatedAt:HH:mm:ss.fff}\n" +
                $"PosX={st.PosX}\nPosY={st.PosY}\nPosZ={st.PosZ}\n" +
                $"SpeedX={st.SpeedX}\nSpeedY={st.SpeedY}\nSpeedZ={st.SpeedZ}",
                "MCU Status");
        }
    }
}
