using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Measurement_MC_App.Helps;
using Measurement_MC_App.Logic;
using Measurement_MC_App.Models;
using Measurement_MC_App.Service;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using static OpenCvSharp.Stitcher;

namespace Measurement_MC_App.ViewModels
{
    public enum OperatingMode
    {
        Calibration,
        Normal,
        EasyAdvanced
    }

    public partial class VisionViewModel : ObservableObject
    {

        [ObservableProperty] private OperatingMode selectedMode = OperatingMode.Normal;

        [ObservableProperty] private int blur = 5;
        [ObservableProperty] private int cannyLow = 60;
        [ObservableProperty] private int cannyHigh = 120;

        [ObservableProperty] private double refWidthMm = 0;
        [ObservableProperty] private double refHeightMm = 0;

     
        [ObservableProperty] private BitmapSource? calibImage;
        [ObservableProperty] private BitmapSource? calibEdges;
        [ObservableProperty] private BitmapSource? normalImage;
        [ObservableProperty] private BitmapSource? normalEdges;


        [ObservableProperty] private PreviewSlot selectedPreview = PreviewSlot.CalibImage;
        [ObservableProperty] private BitmapSource? previewImage;
        [ObservableProperty] private string previewTitle = "CALIB • IMAGE";

        public VisionViewModel()
        {
            UpdatePreviewFromSelected(SelectedPreview);
           
            Camera_SV.Instance.Pub -= Subcribe; 
            Camera_SV.Instance.Pub += Subcribe;
        }
        
        private void Subcribe(object? sender, string e)
        {
            CalibImage = Convert_Image_Helper.BitmapToBitmapSource(Camera_BLL.Instance.Face);
            CalibEdges = Convert_Image_Helper.BitmapToBitmapSource(Camera_BLL.Instance.Edge);
            NormalImage = Convert_Image_Helper.BitmapToBitmapSource(Camera_BLL.Instance.Face);
            NormalEdges = Convert_Image_Helper.BitmapToBitmapSource(Camera_BLL.Instance.Edge);

            OnSelectedPreviewChanged(SelectedPreview);
        }

    
        partial void OnSelectedPreviewChanged(PreviewSlot value)
        {
            UpdatePreviewFromSelected(value);
        }

        [RelayCommand]
        private void SelectPreview(PreviewSlot slot)
        {
            SelectedPreview = slot; 
        }

        private void UpdatePreviewFromSelected(PreviewSlot slot)
        {
            switch (slot)
            {
                case PreviewSlot.CalibImage:
                    PreviewTitle = "CALIB • IMAGE";
                    PreviewImage = CalibImage;
                    break;

                case PreviewSlot.CalibEdges:
                    PreviewTitle = "CALIB • EDGES";
                    PreviewImage = CalibEdges;
                    break;

                case PreviewSlot.NormalImage:
                    PreviewTitle = "NORMAL • IMAGE";
                    PreviewImage = NormalImage;
                    break;

                case PreviewSlot.NormalEdges:
                    PreviewTitle = "NORMAL • EDGES";
                    PreviewImage = NormalEdges;
                    break;
            }
        }

        
        partial void OnBlurChanged(int value)
        {
            if(value % 2 == 0)
            {
                value += 1;
                Blur = value;
            }
            Camera_BLL.Instance.Blur = Blur;

        }

        partial void OnCannyLowChanged(int value)
        {
            Camera_BLL.Instance.CannyL = cannyLow;
        }

        partial void OnCannyHighChanged(int value)
        {
            Camera_BLL.Instance.CannyH = cannyHigh;
          
        }

      
        [RelayCommand]
        private void SetMode(OperatingMode mode)
        {
            if (SelectedMode == mode) return;
            SelectedMode = mode;
            Camera_SV.Instance.Mode = mode;

        }

        [RelayCommand]
        private void ApplyReference()
        {
            if (RefWidthMm <= 0 || RefHeightMm <= 0)
            {
                MessageBox.Show("Reference must be > 0", "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Camera_BLL.Instance.Height_ref = RefHeightMm;
            Camera_BLL.Instance.Width_ref = RefWidthMm;


        }

        partial void OnRefWidthMmChanged(double value)
        {
            
        }

        partial void OnRefHeightMmChanged(double value)
        {

        }
    }
}
