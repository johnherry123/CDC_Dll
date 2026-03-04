using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{

    public partial class TcpSettings : ObservableObject
    {
        [ObservableProperty] private string host = "192.168.0.10";
        [ObservableProperty] private int port = 502;
    }
}
