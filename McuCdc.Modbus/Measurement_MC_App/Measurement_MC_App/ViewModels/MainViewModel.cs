using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using static Measurement_MC_App.ViewModels.TrayCellVM;

namespace Measurement_MC_App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<TrayBoardVM> TrayBoards { get; } = new();

        [ObservableProperty] private string traySubtitle = "—";
        [ObservableProperty] private string traySizeText = "—";

        [ObservableProperty] private int currentTrayCount;
        [ObservableProperty] private int currentRows;
        [ObservableProperty] private int currentCols;

        public MainViewModel()
        {
       
            Generate(trayCount: 6, rows: 13, cols: 13, subtitle: "Model: A17 LTE", defaultState: TrayCellState.Default);
        }


        public void Generate(int trayCount, int rows, int cols, string? subtitle = null, TrayCellState defaultState = TrayCellState.Default)
        {
            RunOnUI(() =>
            {
                trayCount = Math.Max(1, trayCount);
                rows = Math.Max(1, rows);
                cols = Math.Max(1, cols);

                TrayBoards.Clear();

                for (int t = 0; t < trayCount; t++)
                {

                    var board = new TrayBoardVM(
                        trayIndex: t,
                        title: trayCount == 1 ? "Tray" : $"Tray #{t + 1}",
                        rows: rows,
                        cols: cols,
                        defaultState: defaultState
                    );

                    TrayBoards.Add(board);
                }

                CurrentTrayCount = trayCount;
                CurrentRows = rows;
                CurrentCols = cols;

                TraySizeText = $"{trayCount} tray • {rows}×{cols}";
                if (!string.IsNullOrWhiteSpace(subtitle))
                    TraySubtitle = subtitle;
            });
        }


        public void SetCellState(int trayIndex, int row, int col, TrayCellState state)
        {
            RunOnUI(() =>
            {
                if (!TryGetCell(trayIndex, row, col, out var cell)) return;
                cell.State = state;
            });
        }


        public void SetCellStateIndex(int trayIndex, int cellIndex, TrayCellState state)
        {
            RunOnUI(() =>
            {
                if (trayIndex < 0 || trayIndex >= TrayBoards.Count) return;
                var board = TrayBoards[trayIndex];
                if (cellIndex < 0 || cellIndex >= board.Cells.Count) return;

                board.Cells[cellIndex].State = state;
            });
        }

        private bool TryGetCell(int trayIndex, int row, int col, out TrayCellVM cell)
        {
            cell = null!;

            if (trayIndex < 0 || trayIndex >= TrayBoards.Count) return false;
            var board = TrayBoards[trayIndex];

            if (row < 0 || row >= board.Rows) return false;
            if (col < 0 || col >= board.Cols) return false;

            int idx = row * board.Cols + col;
            if (idx < 0 || idx >= board.Cells.Count) return false;

            cell = board.Cells[idx];
            return true;
        }

        [RelayCommand]
        private void TrayClicked(TrayBoardVM? board)
        {
            if (board is null) return;
            MessageBox.Show($"Bạn vừa click: {board.Title} (Index={board.TrayIndex})", "Tray Click");
        }

   
        [RelayCommand]
        private void CellClicked(TrayCellVM? cell)
        {
            if (cell is null) return;
          
           // MessageBox.Show($"Cell: Index={cell.Index} | R={cell.Row} C={cell.Col} | State={cell.State}", "Cell Click");
        }


        [RelayCommand]
        private void DemoPaint()
        {
      
            RunOnUI(() =>
            {
             
                for (int t = 0; t < TrayBoards.Count; t++)
                {
                    var b = TrayBoards[t];
                    for (int i = 0; i < b.Cells.Count; i++)
                        b.Cells[i].State = TrayCellState.Ok; 
                }

        
                SetCellState(0, 0, 0, TrayCellState.Ok);
                SetCellState(0, 1, 1, TrayCellState.Ng);

                if (TrayBoards.Count > 1)
                    SetCellStateIndex(1, 5, TrayCellState.Ng);
            });
        }


        private static void RunOnUI(Action action)
        {
            var app = Application.Current;
            if (app?.Dispatcher is null) { action(); return; }

            var d = app.Dispatcher;
            if (d.CheckAccess()) action();
            else d.Invoke(action, DispatcherPriority.Background);
        }
    }
}
