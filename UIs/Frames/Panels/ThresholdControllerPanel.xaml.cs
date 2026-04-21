using GVisionWpf.GlobalStates;
using GVisionWpf.Visions;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    public partial class ThresholdControllerPanel : UserControl
    {
        public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(nameof(Threshold), typeof(Threshold), typeof(ThresholdControllerPanel), new PropertyMetadata(new Threshold(0, 255)));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(string), typeof(ThresholdControllerPanel), new PropertyMetadata(string.Empty));

        public Threshold Threshold
        {
            get => (Threshold)GetValue(ThresholdProperty);
            set => SetValue(ThresholdProperty, value);
        }

        public string Color
        {
            get => (string)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public string DrawMode { get; set; } = "fill";

        public ThresholdControllerPanel()
        {
            InitializeComponent();
        }

        private void updateThreshold()
        {
            HObject? image = CurrentTeachingWindow.Instance.TeachingImage;

            if (Threshold.MinGray > Threshold.MaxGray || image == null || image.Key == IntPtr.Zero)
            {
                return;
            }

            if (CurrentTeachingWindow.Instance.Window == null || CurrentTeachingWindow.Instance.Window.xHSmartWindow.HalconWindow == null)
            {
                return;
            }

            VisionOperation.Threshold(image, Threshold, out HObject packageRegion);

            CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.ClearWindow();

            if (Color == string.Empty)
            {
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetDraw("fill");
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetColor("white");
            }
            else
            {
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.DispObj(image);
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetDraw(DrawMode);
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetLineWidth(2);
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetColor(Color);
            }

            CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.DispObj(packageRegion);
        }

        private void textBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                updateThreshold();
            }
            catch
            {

            }
            SetValue(ThresholdProperty, Threshold);
        }

        // aml
        public void Button()
        {
            Button_OnClick(null, null);
        }
        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            HObject? image = CurrentTeachingWindow.Instance.TeachingImage;

            if (image == null) return;

            CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.ClearWindow();
            CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.DispObj(image);
        }

        public void Refresh()
        {
            updateThreshold();
        }
    }
}