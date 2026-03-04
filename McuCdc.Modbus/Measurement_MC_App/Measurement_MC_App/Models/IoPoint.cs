using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public partial class IoPoint : ObservableObject
    {
        public IoPoint(string name, string description, bool isOn = false)
        {
            Name = name;
            Description = description;
            this.isOn = isOn;
        }

        public string Name { get; }
        public string Description { get; }

        [ObservableProperty] private bool isOn;
    }
}
