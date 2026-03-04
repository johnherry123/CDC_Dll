using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Measurement_MC_App.Models;
using Measurement_MC_App.Service;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Measurement_MC_App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private  IMcuModbusService _service => McuComHub.Service;

        [ObservableProperty] private bool isConnected;
        [ObservableProperty] private bool isBusy;
        public ComSettings com = App.Host.Services.GetRequiredService<ComSettings>();
        public TcpSettings tcp = App.Host.Services.GetRequiredService<TcpSettings>();

        public string ConnectButtonText => IsConnected ? "DISCONNECT" : "CONNECT";

        public MainWindowViewModel()
        {
            

        }

        partial void OnIsConnectedChanged(bool value)
            => OnPropertyChanged(nameof(ConnectButtonText));

        [RelayCommand(CanExecute = nameof(CanToggleConnect))]
        private async Task ToggleConnect()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                ToggleConnectCommand.NotifyCanExecuteChanged();

                if (!IsConnected)
                {
                    McuComHub.Configure(com.PortName, com.BaudRate, 1);
                    McuComHub.EnsureConnectedAsync();
                    McuComHub.Service.StartListeningAsync();
                    McuComHub.Service.StatusChanged += st =>
                    {

                        McuState.UpdateFrom(st);
                    };
                    Camera_SV.Instance.Connect();
                    Thread.Sleep(500);
                    IsConnected = true;
                    com.IsConnected = true; 

                }
                else
                {
                    _= McuComHub.DisconnectAsync();
                    Camera_SV.Instance.DisconnectBaslerCamera();    
                   IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
                ToggleConnectCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanToggleConnect() => !IsBusy;
    }
}
