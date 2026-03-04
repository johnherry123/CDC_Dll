using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Measurement_MC_App.Models
{
    public partial class ModelSpec : ObservableObject
    {
        public ModelSpec(string _name) => ModelName = _name;

        public ModelSpec(string modelName, int trayCount, int rows, int cols)
        {
            ModelName = modelName;
            TrayCount = trayCount;
            Rows = rows;
            Cols = cols;
        }
        public ModelSpec()
        {

        }

        public string ModelName { get; init; } = "";
        public int TrayCount { get; init; } = 1;
        public int Rows { get; init; }
        public int Cols { get; init; }

        [ObservableProperty] private double nominalX = 14.16;
        [ObservableProperty] private double nominalY = 2.50;

        [ObservableProperty] private double tolXPlus = 0.04;
        [ObservableProperty] private double tolXMinus = 0.05;
    } 
 }
