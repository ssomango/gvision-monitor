using GVisionWpf.UIs.DrawingObjects;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// RoiPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RoiPanel : UserControl
    {
        public Roi Roi
        {
            get => (Roi)GetValue(RoiProperty);
            set => SetValue(RoiProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public EColor Color
        {
            get => (EColor)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty RoiProperty = DependencyProperty.Register(nameof(Roi), typeof(Roi), typeof(RoiPanel), new PropertyMetadata(new Roi("ROI", 0, 0, 100, 100)));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(RoiPanel), new PropertyMetadata("ROI"));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(EColor), typeof(RoiPanel), new PropertyMetadata(EColor.Orange));

        private DrawingObjectRoiWithText? drawingObjectTextRoi;

        public RoiPanel()
        {
            InitializeComponent();
        }

        ~RoiPanel()
        {
            this.drawingObjectTextRoi?.Delete();
        }

        public void CreateRoi()
        {
            CreateRoi(Roi);
        }

        public void CreateRoi(Roi roi)
        {
            Roi = roi;

            this.drawingObjectTextRoi?.Detach();
            this.drawingObjectTextRoi = new DrawingObjectRoiWithText(roi, Color);
            this.drawingObjectTextRoi.Create();
            Attach();
        }

        private void roiCreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (Roi == null)
            {
                Roi = new Roi(Label, 500, 500, 1000, 1000);
            }

            if (Roi.Row1 > Roi.Row2)
            {
                (Roi.Row1, Roi.Row2) = (Roi.Row2, Roi.Row1);
            }

            if (Roi.Col1 > Roi.Col2)
            {
                (Roi.Col1, Roi.Col2) = (Roi.Col2, Roi.Col1);
            }

            CreateRoi(Roi);
        }

        public void Detach()
        {
            this.xRoiCreateButton.IsEnabled = true;
            this.drawingObjectTextRoi?.Detach();
        }

        public void Attach()
        {
            if (this.drawingObjectTextRoi == null)
            {
                CreateRoi();
            }

            this.xRoiCreateButton.IsEnabled = false;
            this.drawingObjectTextRoi!.Attach();
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            HTuple param = new HTuple("row1", "column1", "row2", "column2");
            HTuple value = new HTuple(Roi.Row1, Roi.Col1, Roi.Row2, Roi.Col2);
            this.drawingObjectTextRoi?.SetParameter(param, value);
            this.drawingObjectTextRoi?.UpdateTextPosition();
        }
    }
}