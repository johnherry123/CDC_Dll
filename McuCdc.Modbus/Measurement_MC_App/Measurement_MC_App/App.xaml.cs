using Measurement_MC_App.Models;
using Measurement_MC_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Measurement_MC_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost Host { get; private set; } = null!;
        public static MainViewModel MainVM { get; } = new MainViewModel();
        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }
        public App()
        {
            Host = new HostBuilder()
                 .ConfigureServices(services =>
                 {
                     services.AddSingleton<ComSettings>();
                     services.AddSingleton<TcpSettings>();
                    

                     services.AddSingleton<SettingsViewModel>();
  

                 })
                 .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host.StartAsync();
            base.OnStartup(e);
        }
        protected override async void OnExit(ExitEventArgs e)
        {
            await Host.StopAsync();
            Host.Dispose();
            base.OnExit(e);
        }
    }

}
