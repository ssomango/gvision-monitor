using GVisionWpf.GlobalStates;
using GVisionWpf.UIs.DrawingObjects;
using GVisionWpf.Visions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// RoiPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GridRoiPanel : UserControl
    {
        public static readonly DependencyProperty RowSizeProperty = DependencyProperty.Register(nameof(RowSize), typeof(int), typeof(GridRoiPanel), new PropertyMetadata(2));
        public static readonly DependencyProperty ColumnSizeProperty = DependencyProperty.Register(nameof(ColumnSize), typeof(int), typeof(GridRoiPanel), new PropertyMetadata(2));
        public static readonly DependencyProperty RoiProperty = DependencyProperty.Register(nameof(Roi), typeof(Roi), typeof(GridRoiPanel), new PropertyMetadata(null));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(GridRoiPanel), new PropertyMetadata("ROI"));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(EColor), typeof(GridRoiPanel), new PropertyMetadata(EColor.Orange));

        private DrawingObjectRoiWithText? drawingObjectRoiWithText;

        #region Property

        public int RowSize
        {
            get => (int)GetValue(RowSizeProperty);
            set => SetValue(RowSizeProperty, value);
        }

        public int ColumnSize
        {
            get => (int)GetValue(ColumnSizeProperty);
            set => SetValue(ColumnSizeProperty, value);
        }

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

        #endregion

        public GridRoiPanel()
        {
            InitializeComponent();
        }

        ~GridRoiPanel()
        {
            this.drawingObjectRoiWithText?.Delete();
        }

        public void CreateRoi(Roi roi)
        {
            Roi = roi;

            this.drawingObjectRoiWithText?.Detach();
            this.drawingObjectRoiWithText = new DrawingObjectRoiWithText(roi, Color);
            this.drawingObjectRoiWithText.Create();
            this.drawingObjectRoiWithText.Attach();
            displayGridRoi();
        }

        private void roiCreateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Roi == null)
            {
                Roi = new Roi("GRID ROI", 500, 500, 1000, 1000);
            }

            CreateRoi(Roi);
        }
        
        // aml
        public void roiCreateButton()
        {
            roiCreateButton_Click(null, null);
        }
        private void displayGridRoi()
        {
            if (Roi == null) return;

            VisionOperation.PartitionRectangle(Roi, RowSize, ColumnSize, out HObject partitionRect);

            CurrentTeachingWindow.Instance.Window?.Display(CurrentTeachingWindow.Instance.TeachingImage);
            CurrentTeachingWindow.Instance.Window?.Display(partitionRect, Color, "margin", 2);
        }

        public void Detach()
        {
            this.xRoiCreateButton.IsEnabled = true;
            this.drawingObjectRoiWithText?.Detach();
        }

        public void Attach()
        {
            if (Roi == null) return;

            if (this.drawingObjectRoiWithText == null)
            {
                CreateRoi(Roi);
            }

            this.xRoiCreateButton.IsEnabled = false;
            this.drawingObjectRoiWithText?.Attach();
            displayGridRoi();
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Roi == null) return;

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

            if (!CurrentTeachingWindow.Instance.Window!.IsWindowLoaded) return;


            HTuple param = new HTuple("row1", "column1", "row2", "column2");
            HTuple value = new HTuple(Roi.Row1, Roi.Col1, Roi.Row2, Roi.Col2);
            this.drawingObjectRoiWithText?.SetParameter(param, value);
            this.drawingObjectRoiWithText?.UpdateTextPosition();

            displayGridRoi();
        }

        private void roiDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Roi = null;

            Detach();
            CurrentTeachingWindow.Instance?.Window?.Clear();
            CurrentTeachingWindow.Instance?.Window?.Display(CurrentTeachingWindow.Instance.TeachingImage);
        }
    }
}