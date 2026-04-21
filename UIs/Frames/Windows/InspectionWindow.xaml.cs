using GVisionWpf.Events.Message;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// InspectionWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InspectionWindow : FloatingWindowBase
    {
        private List<RenderableInspectionResult>? curRenderableResults;

        private bool shouldShowText = true;
        private bool shouldShowRegion = true;
        private bool shouldShowGrayValue = true;

        public InspectionWindow(string windowName) : base(windowName, true)
        {
            InitializeComponent();
        }

        #region Events

        private void zoomInButton_Click(object sender, RoutedEventArgs e)
        {
            this.xVisionWindow.ZoomImage(20);
        }

        private void zoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            this.xVisionWindow.ZoomImage(-20);
        }

        private void zoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            this.xVisionWindow.SetFullImagePart();
        }

        private void textDisplayButton_Click(object sender, RoutedEventArgs e)
        {
            this.shouldShowText = !this.shouldShowText;
            ((Button)sender).Background = this.shouldShowText ? Brushes.White : Brushes.LightGray;

            ReDisplayResult();
        }

        private void overlayButton_Click(object sender, RoutedEventArgs e)
        {
            this.shouldShowRegion = !this.shouldShowRegion;
            ((Button)sender).Background = this.shouldShowRegion ? Brushes.White : Brushes.LightGray;

            ReDisplayResult();
        }

        private void grayValueButton_Click(object sender, RoutedEventArgs e)
        {
            this.shouldShowGrayValue = !this.shouldShowGrayValue;
            ((Button)sender).Background = this.shouldShowGrayValue ? Brushes.White : Brushes.LightGray;

            this.xVisionWindow.ToggleGrayValueMode();
        }

        private void deviceViewVisibleButton_Click(object sender, RoutedEventArgs e)
        {
            switch (this.WindowTitle)
            {
                case "Mapping Inspection":
                    MapDeviceViewViewModel.Instance.Visibility = (MapDeviceViewViewModel.Instance.Visibility == Visibility.Hidden) ? Visibility.Visible : Visibility.Hidden;
                    break;
                case "PRS Inspection":
                    PrsDeviceViewViewModel.Instance.Visibility = (PrsDeviceViewViewModel.Instance.Visibility == Visibility.Hidden) ? Visibility.Visible : Visibility.Hidden;
                    break;
                default:
                    break;
            }
        }

        #endregion

        public void ClearWindow()
        {
            this.xVisionWindow.Clear();
        }

        public void ReDisplayResult()
        {
            if (this.curRenderableResults != null)
            {
                DisplayResult(this.curRenderableResults);
            }
        }

        public void DisplayResult(List<RenderableInspectionResult> renderableResults)
        {
            this.curRenderableResults = renderableResults;

            renderableResults.ForEach(renerable => this.xVisionWindow.Display(renderableResults));
        }

        public void DisplayResult(RenderableInspectionResult renderable)
        {
            this.curRenderableResults = [renderable];

            xVisionWindow.Display(renderable);
        }
    }
}