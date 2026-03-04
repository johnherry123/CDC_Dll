using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.ViewModels
{

    public sealed class TrayBoardVM
    {
        public int TrayIndex { get; }
        public string Title { get; }
        public int Rows { get; }
        public int Cols { get; }
        public ObservableCollection<TrayCellVM> Cells { get; } = new();

        public TrayBoardVM(int trayIndex, string title, int rows, int cols, TrayCellState defaultState = TrayCellState.Ng)
        {
            TrayIndex = trayIndex;
            Title = title;
            Rows = rows;
            Cols = cols;

            int idx = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Cells.Add(new TrayCellVM(trayIndex, r, c, defaultState));
                    idx++;
                }
        }
    }
}
