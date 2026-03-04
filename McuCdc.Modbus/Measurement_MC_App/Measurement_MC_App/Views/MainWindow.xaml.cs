using Measurement_MC_App.Logic;
using Measurement_MC_App.Views.Theme;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Measurement_MC_App.Views
{
    public partial class MainWindow : Window
    {
        private readonly List<NavItem> _items;

        public MainWindow()
        {
            InitializeComponent();

            // sync toggle theo theme hiện tại
            var current = ThemeManager.DetectCurrentTheme();
            ThemeToggle.IsChecked = current == "Light";
            ThemeToggle.Content = current == "Light" ? "☾" : "☀";

            _items = new List<NavItem>();

            // tạo view bình thường
            _items.Add(new NavItem("Main", GeoHome(), new MainView()));
            _items.Add(new NavItem("Motor", GeoGear(), new MotorView()));
            _items.Add(new NavItem("Vision", GeoCamera(), new VisionView()));
            _items.Add(new NavItem("Dash", GeoChart(), new DashView()));

            // Settings: bọc try/catch để thấy lỗi XAML/Resource/VM
            try
            {
                _items.Add(new NavItem("Settings", GeoSettings(), new SettingsView()));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(ex.ToString(), "SettingsView failed to load");
            }

            NavList.ItemsSource = _items;

            NavList.SelectedIndex = 0;
            MainContent.Content = _items[0].View;
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is not NavItem item) return;

            try
            {
                MainContent.Content = item.View;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(ex.ToString(), "Navigate failed");
            }
        }

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Apply("Light");
            ThemeToggle.Content = "☾";
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Apply("Dark");
            ThemeToggle.Content = "☀";
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMax_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { BtnMax_Click(sender, e); return; }
            DragMove();
        }

        // Icons
        private static Geometry GeoHome() => Geometry.Parse("M12,3 L3,10 L3,21 L10,21 L10,14 L14,14 L14,21 L21,21 L21,10 Z");
        private static Geometry GeoGear() => Geometry.Parse("M12,2 L14,3 L15,5 L17,6 L19,5 L20,7 L18,9 L19,11 L21,12 L21,14 L19,15 L18,17 L20,19 L19,21 L17,20 L15,21 L14,23 L12,24 L10,23 L9,21 L7,20 L5,21 L4,19 L6,17 L5,15 L3,14 L3,12 L5,11 L6,9 L4,7 L5,5 L7,6 L9,5 L10,3 Z M12,9 A3,3 0 1 1 11.99,9 Z");
        private static Geometry GeoCamera() => Geometry.Parse("M6,7 L9,7 L10,5 L14,5 L15,7 L18,7 A3,3 0 0 1 21,10 L21,18 A3,3 0 0 1 18,21 L6,21 A3,3 0 0 1 3,18 L3,10 A3,3 0 0 1 6,7 Z M12,10 A4,4 0 1 1 11.99,10 Z");
        private static Geometry GeoChart() => Geometry.Parse("M4,20 L20,20 L20,4 L18,4 L18,18 L4,18 Z M6,16 L9,13 L12,15 L16,9 L18,11 L18,14 L16,12 L12,17 L9,15 L6,18 Z");
        private static Geometry GeoSettings() => Geometry.Parse("M12,2 A10,10 0 1 0 12,22 A10,10 0 1 0 12,2 Z M12,7 A1.5,1.5 0 1 1 12,10 A1.5,1.5 0 1 1 12,7 Z M11,11 L13,11 L13,18 L11,18 Z");

        Handler_BLL Handler = new Handler_BLL();
        private void Button_Click(object sender, RoutedEventArgs e) => Handler.Handler_Run();
    }
}
