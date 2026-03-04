using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
  
        public partial class ComSettings : ObservableObject
        {
            [ObservableProperty] private string portName = "COM4";
            [ObservableProperty] private int baudRate = 115200;
            [ObservableProperty] private int dataBits = 8;
            [ObservableProperty] private string parity = "None";     
            [ObservableProperty] private string stopBits = "One";   
            [ObservableProperty] private bool isConnected = false;
        }
    
}
