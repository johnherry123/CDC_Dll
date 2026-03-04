using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.ViewModels
{

    public enum TrayCellState { Default = 2 , Ok = 1, Ng = 0 }

    public class TrayCellVM : ObservableObject
    {
        public int Index { get; }
        public int Row { get; }
        public int Col { get; }
        public string Label { get; }

        private TrayCellState _state;
        public TrayCellState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public TrayCellVM(int index, int row, int col, TrayCellState defaultState = TrayCellState.Ng)
        {
            Index = index;
            Row = row;
            Col = col;
            Label = $"{index + 1:000}";
            _state = defaultState;
        }
    }

}
