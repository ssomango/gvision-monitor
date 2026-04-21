using GVisionWpf.UIs.DrawingObjects;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    public partial class RoiPanelV2 : UserControl
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

        public Visibility HeaderVisibility
        {
            get { return (Visibility)GetValue(HeaderVisibilityProperty); }
            set { SetValue(HeaderVisibilityProperty, value); }
        }

        public static readonly DependencyProperty RoiProperty = DependencyProperty.Register(nameof(Roi), typeof(Roi), typeof(RoiPanelV2), new PropertyMetadata(null));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(RoiPanelV2), new PropertyMetadata("ROI"));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(EColor), typeof(RoiPanelV2), new PropertyMetadata(EColor.Orange));
        public static readonly DependencyProperty HeaderVisibilityProperty = DependencyProperty.Register("HeaderVisibility", typeof(Visibility), typeof(RoiPanelV2), new PropertyMetadata(Visibility.Visible));

        private DrawingObjectRoiWithText? drawingObjectTextRoi;

        public RoiPanelV2()
        {
            InitializeComponent();
        }

        ~RoiPanelV2()
        {
            this.drawingObjectTextRoi?.Delete();
        }

        public void CreateRoi()
        {
            if (Roi == null) return;

            CreateRoi(Roi);
        }

        public void CreateRoi(Roi roi)
        {
            if (roi == null) return;

            Roi = roi;

            this.drawingObjectTextRoi?.Detach();
            this.drawingObjectTextRoi = new DrawingObjectRoiWithText(roi, Color);
            this.drawingObjectTextRoi.Create();
            Attach();
        }

        public void roiCreateButton()
        {
            roiCreateButton_Click(null, null);
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

        public void roiDeleteButton()
        {
            roiDeleteButton_Click(null, null);  // 버튼 클릭 이벤트 메서드 호출
        }
        private void roiDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Roi = null;
            Detach();
        }

        public void Detach()
        {
            this.xRoiCreateButton.IsEnabled = true;
            this.drawingObjectTextRoi?.Detach();
        }

        public void Attach()
        {
            if (Roi == null) return;

            if (this.drawingObjectTextRoi == null)
            {
                CreateRoi();
            }

            this.xRoiCreateButton.IsEnabled = false;
            this.drawingObjectTextRoi!.Attach();
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Roi == null) return;

            HTuple param = new HTuple("row1", "column1", "row2", "column2");

            HTuple value = new HTuple(Roi.Row1, Roi.Col1, Roi.Row2, Roi.Col2);
            this.drawingObjectTextRoi?.SetParameter(param, value);
            this.drawingObjectTextRoi?.UpdateTextPosition();
        }
    }
}