using GVisionWpf.GlobalStates;
using GVisionWpf.Models.UiModels;
using GVisionWpf.UIs.DrawingObjects;
using GVisionWpf.Visions;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// MappingRoiDataListPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MappingRoiDataListPanel : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(MappingRoiDataListPanel), new PropertyMetadata(null));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(MappingRoiDataListPanel), new PropertyMetadata("ROI"));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(EColor), typeof(MappingRoiDataListPanel), new PropertyMetadata(EColor.Gold));

        private readonly HTuple ocrHandle;
        private readonly List<DrawingObjectRoiWithText> drawingObjectTextRois = new List<DrawingObjectRoiWithText>(16);

        #region Property

        public ThresholdControllerPanel? ThresholdPanel { get; set; }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
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

        public MappingRoiDataListPanel()
        {
            InitializeComponent();
            HOperatorSet.ReadOcrClassCnn("Universal_0-9A-Z_NoRej.occ", out this.ocrHandle);
        }

        ~MappingRoiDataListPanel()
        {
            deleteAll();
        }

        private void deleteAll()
        {
            foreach (DrawingObjectRoiWithText drawingObject in this.drawingObjectTextRois)
            {
                drawingObject.Detach();
                drawingObject.Delete();
            }
        }

        public void DetachAll()
        {
            foreach (DrawingObjectRoiWithText drawingObject in this.drawingObjectTextRois)
            {
                drawingObject.Detach();
            }
        }

        public void AttachAll()
        {
            ObservableCollection<MarkItemSource> collection = (ObservableCollection<MarkItemSource>)ItemsSource;

            if (this.drawingObjectTextRois.Count != collection.Count)
            {
                deleteAll();

                List<MarkItemSource> itemSourceSnapShot = collection.ToList();
                collection.Clear();

                foreach (MarkItemSource markItemSource in itemSourceSnapShot)
                {
                    CreateRoi(markItemSource.Roi);
                }

                return;
            }

            foreach (DrawingObjectRoiWithText drawingObject in this.drawingObjectTextRois)
            {
                drawingObject.Attach();
            }
        }


        public void CreateRoi(Roi roi)
        {
            DrawingObjectRoiWithText drawingObject = new DrawingObjectRoiWithText(roi, Color);
            drawingObject.Create();
            drawingObject.Attach();
            this.drawingObjectTextRois.Add(drawingObject);

            ObservableCollection<MarkItemSource>? collection = ItemsSource as ObservableCollection<MarkItemSource>;

            collection?.Add(new MarkItemSource
            {
                Roi = roi,
                OcrText = "ABC"
            });

            Application.Current.Dispatcher.InvokeAsync(() => { this.xTextRoiDataGrid.SelectedIndex = this.drawingObjectTextRois.Count - 1; });
        }

        private void textRoiAddButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<MarkItemSource>? collection = ItemsSource as ObservableCollection<MarkItemSource>;
            List<int> roiNumberList = collection?.Select(item => int.Parse(item.Roi.Name.Split(" ").Last())).ToList();
            int roiCount = roiNumberList == null || roiNumberList.Count() <= 0 ? 1 : roiNumberList.Max() + 1;

            Roi defaultRoi = new Roi("ROI " + roiCount, 500, 500, 1000, 1000);
            CreateRoi(defaultRoi);
        }

        private void textRoiDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<MarkItemSource>? collection = ItemsSource as ObservableCollection<MarkItemSource>;

            int index = this.xTextRoiDataGrid.SelectedIndex;
            if (index < 0 || index >= collection?.Count)
            {
                return;
            }

            this.drawingObjectTextRois[index].Delete();

            this.drawingObjectTextRois.RemoveAt(index);
            collection?.RemoveAt(index);

            Application.Current.Dispatcher.InvokeAsync(() => { this.xTextRoiDataGrid.SelectedIndex = this.drawingObjectTextRois.Count - 1; });
        }

        private void textRoiResetButton_Click(object sender, RoutedEventArgs e)
        {
            deleteAll();

            ObservableCollection<MarkItemSource>? collection = ItemsSource as ObservableCollection<MarkItemSource>;
            foreach (MarkItemSource makItemSource in collection)
            {
                makItemSource.SampleImage?.Dispose();
            }
            collection.Clear();
            this.drawingObjectTextRois.Clear();

            ItemsSource = collection;
        }

        private T findVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = findVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTeachingWindow.Instance.TeachingImage == null || this.ThresholdPanel == null)
            {
                return;
            }

            if (ItemsSource is not ObservableCollection<MarkItemSource> collection)
            {
                return;
            }

            for (int i = 0; i < collection.Count; i++)
            {
                MarkItemSource markItemSource = collection[i];
                if (markItemSource.IsOcrMode == false)
                {
                    VisionOperation.ReduceDomain(CurrentTeachingWindow.Instance.TeachingImage, markItemSource.Roi, out HObject image);
                    HOperatorSet.CropDomain(image, out image);

                    HObject region = VisionOperation.GetConnectedTextRegion(image, ThresholdPanel.Threshold);
                    VisionOperation.ReduceDomain(image, region, out image);

                    //markItemSource.SampleImage?.Dispose();
                    markItemSource.SampleImage = image;
                    markItemSource.OcrText = "";

                    DataGridRow row = (DataGridRow)this.xTextRoiDataGrid.ItemContainerGenerator.ContainerFromIndex(i);
                    if (row == null)
                    {
                        continue;
                    }

                    // HSmartWindowControlWPF smartWindow = (HSmartWindowControlWPF)row.FindName("windowControl");
                    // DataGridRow 내부의 시각적 트리를 탐색하여 xHSmartWindowControlWpf 컨트롤을 찾습니다.
                    HSmartWindowControlWPF smartWindow = findVisualChild<HSmartWindowControlWPF>(row);
                    smartWindow.HalconWindow.SetWindowParam("region_quality", "good");
                    smartWindow.HalconWindow.ClearWindow();
                    smartWindow.HalconWindow.DispObj(image);
                    smartWindow.SetFullImagePart();
                }
                else
                {
                    VisionOperation.ReduceDomain(CurrentTeachingWindow.Instance.TeachingImage, markItemSource.Roi, out HObject image);
                    markItemSource.SampleImage = image;
                    HObject connectedText = VisionOperation.GetConnectedTextRegion(image, ThresholdPanel.Threshold);
                    markItemSource.OcrText = VisionOperation.GetOcredText(image, connectedText, ".*", this.ocrHandle);
                    // MessageBox.Show(markItemSource.OcrText);
                }

                VisionOperation.ReduceDomain(CurrentTeachingWindow.Instance.TeachingImage, markItemSource.Roi, out HObject reducedImage);

                HObject connectedRegion = VisionOperation.GetConnectedTextRegion(reducedImage, ThresholdPanel.Threshold);
                VisionOperation.ReduceDomain(reducedImage, connectedRegion, out reducedImage);

                HOperatorSet.SmallestRectangle1(connectedRegion, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                HOperatorSet.GenRectangle1(out HObject charBoxs, row1, col1, row2, col2);
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetDraw("margin");
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.SetColor("pink");
                CurrentTeachingWindow.Instance.Window!.xHSmartWindow.HalconWindow.DispObj(charBoxs);
            }

            ItemsSource = collection;
        }

        private void textRoiDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            // TODO: 선택하는 기능 넣어야함
        }

        private void onCurrentCellChanged(object? sender, EventArgs e)
        {
            // TODO: 여기에 넣을거 있을까? 아직 없었음
        }

        // aml
        public void ExecuteAction(string action)
        {
            Debug.WriteLine("ExecuteAction 들어옴");
            switch (action)
            {
                case "ADD":
                    textRoiAddButton_Click(this, new RoutedEventArgs());
                    break;

                case "DELETE":
                    textRoiDeleteButton_Click(this, new RoutedEventArgs());
                    break;

                case "RESET":
                    textRoiResetButton_Click(this, new RoutedEventArgs());
                    break;

                case "READ":
                    readButton_Click(this, new RoutedEventArgs());
                    break;

                default:
                    Debug.WriteLine($"[RoiDataListPanel] Unknown action: {action}");
                    break;
            }
        }
    }
}