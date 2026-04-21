using GVisionWpf.GlobalStates;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// TeachingToolsPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TeachingToolsPanel : UserControl
    {
        private CurrentTeachingWindow currentTeachingWindow = CurrentTeachingWindow.Instance;

        public TeachingToolsPanel()
        {
            InitializeComponent();
        }

        private void zoomInMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            this.currentTeachingWindow.Window.ZoomImage(20);
        }

        private void zoomOutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            this.currentTeachingWindow.Window.ZoomImage(-20);
        }

        private void resetZoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            this.currentTeachingWindow.Window.SetFullImagePart();
        }


        private void grayValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            this.currentTeachingWindow.Window.ToggleGrayValueMode();
        }

        private void rulerButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            this.currentTeachingWindow.Window.ToggleRulerMode();
        }

        private void lineProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            this.currentTeachingWindow.Window.ToggleLineProfileMode();
        }

        private void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentTeachingWindow.Window == null) { return; }

            HObject image = this.currentTeachingWindow.TeachingImage!;

            DateTime today = DateTime.Now;

            String baseDirectory = "DB/Images";
            String date = today.ToString("yyyy-MM-dd");
            String time = today.ToString("HHmmssfff");
            String inspectionType = this.currentTeachingWindow.InspectionType.ToString().ToLower();

            String fullDirectory = Path.Combine(new string[] { baseDirectory, date, "teaching", inspectionType });

            String fileType = ".png";
            String fileName = time + "-" + GlobalSetting.Instance.DeviceInfo.RecipeName.ToLower() + fileType;

            Directory.CreateDirectory(fullDirectory);
            HOperatorSet.WriteImage(image, "png fastest", 0, fullDirectory + "/" + fileName);
        }
    }
}
