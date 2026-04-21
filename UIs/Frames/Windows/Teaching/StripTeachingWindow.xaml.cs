using System;
using System.Windows;
using System.Windows.Controls;
using GVisionWpf.GlobalStates;
using GVisionWpf.Types;
using GVisionWpf.UIs.ViewModels.Teaching;
using HalconDotNet;

namespace GVisionWpf.UIs.Frames.Windows.Teaching
{
    /// <summary>
    /// StripTeachingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StripTeachingWindow : Window
    {
        private HObject? teachingImage;
        private bool isGrayValueOn = false;

        public StripTeachingWindow()
        {
            InitializeComponent();
            Loaded += onLoaded;

            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;
            CurrentTeachingWindow.Instance.InspectionType = EInspection.Strip;
        }

        #region Property

        public HObject? TeachingImage
        {
            get => this.teachingImage;
            set
            {
                if (DataContext is StripTeachingViewModel viewModel)
                {
                    viewModel.TeachingImage = value;
                }

                this.teachingImage = value;
            }
        }

        #endregion

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.xTabControl.SelectedIndex <= 0)
            {
                return;
            }

            this.xTabControl.SelectedIndex--;
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.xTabControl.SelectedIndex >= this.xTabControl.Items.Count - 1)
            {
                return;
            }

            this.xTabControl.SelectedIndex++;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            Close();
        }

        private void finishButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            Close();
        }
        private void onLoaded(object sender, RoutedEventArgs e)
        {
            StripTeachingViewModel viewModel = (DataContext as StripTeachingViewModel)!;
            viewModel.VisionWindow = this.xVisionWindow;
            viewModel.TeachingImage = TeachingImage;
            clearImage();


            xRoiListPanel.AttachAll();
        }

        private void findCodeButton_OnClick(object sender, RoutedEventArgs e)
        {
            // clearImage();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.xVisionWindow.xHSmartWindow.HalconWindow == null)
            {
                return;
            }

            if (e.Source is TabControl)
            {
                clearImage();
            }

            attachDrawingObjectOfCurrentTab(this.xTabControl.SelectedIndex);
        }

        private void clearImage()
        {
            this.xVisionWindow.Clear();
            this.xVisionWindow.Display(this.teachingImage!);
            this.xVisionWindow.SetFullImagePart();
        }

        private void attachDrawingObjectOfCurrentTab(int tabIndex)
        {
            switch (tabIndex)
            {

            }
        }
    }
}
