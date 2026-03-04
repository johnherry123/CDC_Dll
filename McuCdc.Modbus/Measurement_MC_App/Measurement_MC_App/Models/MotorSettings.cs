using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public partial class MotionSettings : ObservableObject
    {

        [ObservableProperty] private double speedX = 50;
        [ObservableProperty] private double speedY = 50;
        [ObservableProperty] private double speedZ = 50;


        [ObservableProperty] private double maxX = 550;
        [ObservableProperty] private double maxY = 280;
        [ObservableProperty] private double maxZ = 120;
    }
}
