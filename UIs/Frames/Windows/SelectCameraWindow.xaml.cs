using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GVisionWpf.Types;
using GVisionWpf.UIs.ViewModels;
using static GVisionWpf.UIs.Frames.Windows.SelectCameraWindow;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// SelectCameraWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SelectCameraWindow : Window
    {
        private string? SelectedCamera { get; set; }

        public ECamera DialogResult2;

        private SelectCameraViewModel viewModel;

        public SelectCameraWindow() {}

        public SelectCameraWindow(List<ECamera> cameras)
        {
            InitializeComponent();

            viewModel = new SelectCameraViewModel(cameras);
            this.DataContext = viewModel;
        }

        private void cameraButton_Onclick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                SelectedCamera = button.Content.ToString();
                DialogResult = true;
                DialogResult2 = (ECamera)Enum.Parse(typeof(ECamera), SelectedCamera);
                Close();
            }
        }

        private void previewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
