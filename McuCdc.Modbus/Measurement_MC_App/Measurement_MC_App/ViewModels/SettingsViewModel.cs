using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Measurement_MC_App.Models;
using Measurement_MC_App.Service;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Measurement_MC_App.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {

        public ComSettings ComSettings { get; }
        public TcpSettings TcpSettings { get; }

        [ObservableProperty] private string? comPort;
        [ObservableProperty] private string? comBaud;
        [ObservableProperty] private string? tcpHost;
        [ObservableProperty] private string? tcpPort;

        public ObservableCollection<string> AvailableComPorts { get; } = new();
        public ObservableCollection<string> AvailableBaudRates { get; } = new()
    {
        "9600","19200","38400","57600","115200","230400","460800","921600"
    };

        public SettingsViewModel(ComSettings comSettings, TcpSettings tcpSettings)
        {
            ComSettings = comSettings;
            TcpSettings = tcpSettings;

            RefreshComPorts();

            ComPort = !string.IsNullOrWhiteSpace(ComSettings.PortName)
                        ? ComSettings.PortName
                        : AvailableComPorts.FirstOrDefault();

            ComBaud = (ComSettings.BaudRate > 0 ? ComSettings.BaudRate : 115200).ToString();

            TcpHost = string.IsNullOrWhiteSpace(TcpSettings.Host) ? "127.0.0.1" : TcpSettings.Host;
            TcpPort = (TcpSettings.Port > 0 ? TcpSettings.Port : 502).ToString();
        }

        [RelayCommand]
        private Task SetCom()
        {
            var port = ComPort?.Trim();
            if (string.IsNullOrWhiteSpace(port)) return Task.CompletedTask;

            if (!int.TryParse(ComBaud?.Trim(), out var baud) || baud <= 0) baud = 115200;

            ComSettings.PortName = port;
            ComSettings.BaudRate = baud;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task SetTcp()
        {
            try
            {
                var host = TcpHost?.Trim();
                if (string.IsNullOrWhiteSpace(host))
                    throw new InvalidOperationException("TCP Host đang trống.");

                if (!int.TryParse(TcpPort?.Trim(), out var port) || port <= 0 || port > 65535)
                    throw new InvalidOperationException("TCP Port không hợp lệ (1..65535).");

                TcpSettings.Host = host;
                TcpSettings.Port = port;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void RefreshComPorts()
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            AvailableComPorts.Clear();
            foreach (var p in ports) AvailableComPorts.Add(p);

            if (ComPort == null || !AvailableComPorts.Contains(ComPort))
                ComPort = AvailableComPorts.FirstOrDefault();
        }
        public MotionSettings Motor { get; } = new MotionSettings();
        private IMcuModbusService service => McuComHub.Service;
        [RelayCommand]
        private async Task SetMotor()
        {
            try
            {

                if (Motor.MaxX < 0) Motor.MaxX = 0;
                if (Motor.MaxY < 0) Motor.MaxY = 0;
                if (Motor.MaxZ < 0) Motor.MaxZ = 0;
                if (Motor.SpeedX < 0) Motor.SpeedX = 0;
                if (Motor.SpeedY < 0) Motor.SpeedY = 0;
                if (Motor.SpeedZ < 0) Motor.SpeedZ = 0;
                if (Motor.SpeedX > 50) Motor.SpeedX = 50;
                if (Motor.SpeedY > 50) Motor.SpeedY = 50;
                if (Motor.SpeedZ > 50) Motor.SpeedZ = 50;
                ushort xM = ToU16(Motor.MaxX * 100);
                ushort yM = ToU16(Motor.MaxY * 100);
                ushort zM = ToU16(Motor.MaxZ * 100);
                ushort xS = ToU16(Motor.SpeedX * 1000);
                ushort yS = ToU16(Motor.SpeedY * 1000);
                ushort zS = ToU16(Motor.SpeedZ * 1000);



                await service.SetSpeedTarXAsync(xS);
                await service.SetSpeedTarYAsync(yS);
                await service.SetSpeedTarZAsync(zS);
                await service.SetMaxXAsync(xM);
                await service.SetMaxYAsync(yM);
                await service.SetMaxZAsync(zM);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
        }
        private static ushort ToU16(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return 0;
            if (v < 0) return 0;
            if (v > ushort.MaxValue) return ushort.MaxValue;
            return (ushort)Math.Round(v);
        }

        
    }
}
