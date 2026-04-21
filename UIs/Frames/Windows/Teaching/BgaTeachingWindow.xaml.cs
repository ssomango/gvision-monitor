using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.UIs.ViewModels.Teaching;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Windows.Teaching
{
    /// <summary>
    /// BgaTeachingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BgaTeachingWindow : Window
    {
        private HObject? teachingImage;

        private BgaTeachingViewModel viewModel;

        #region Property

        public HObject? TeachingImage
        {
            get => this.teachingImage;
            set
            {
                viewModel.TeachingImage = value;
                CurrentTeachingWindow.Instance.TeachingImage = value;
                this.teachingImage = value;
            }
        }

        #endregion

        public BgaTeachingWindow()
        {
            InitializeComponent();
            registerEvents();
            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;
            CurrentTeachingWindow.Instance.InspectionType = EInspection.Bga;

            viewModel = (BgaTeachingViewModel)DataContext;  
            Loaded += onLoaded;
        }

        ~BgaTeachingWindow() => unRegisterEvents();

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            BgaTeachingViewModel viewModel = (DataContext as BgaTeachingViewModel)!;
            viewModel.VisionWindow = this.xVisionWindow;
            viewModel.TeachingImage = TeachingImage;
            clearImage();
        }

        #region Events
        private void registerEvents()
        {
            xSawOffsetListPanel.PointsUpdated += xPointPickPanel_PointSelected;
        }

        private void unRegisterEvents()
        {
            xSawOffsetListPanel.PointsUpdated -= xPointPickPanel_PointSelected;
        }

        private void xPointPickPanel_PointSelected(object? sender, EventArgs e)
        {
            clearImage();

            if (e is ItemEventArgs<List<Models.Visions.Point>> { Item: var points })
            {
                displaySelectedPoints(points);
            }
        }
        private void displaySelectedPoints(List<Models.Visions.Point> points)
        {
            if (points.IsNullOrEmpty()) return;

            foreach (var point in points)
            {
                HObject pointRegion = point.GenReticle();
                using (pointRegion)
                {
                    CurrentTeachingWindow.Instance.Window?.Display(pointRegion, EColor.Green);
                }
            }
        }
        #endregion

        private void displaySelectedPoints()
        {
            clearImage();

            if (xSawOffsetListPanel.SawOffsetItems.IsNullOrEmpty()) return;

            foreach (SawOffsetItem item in xSawOffsetListPanel.SawOffsetItems)
            {
                HObject pointRegion = item.SawOffsetTargetPoint.GenReticle();

                using (pointRegion)
                {
                    CurrentTeachingWindow.Instance?.Window?.Display(pointRegion, EColor.Green);
                }
            }
        }
        private void attachDrawingObjectOfCurrentTab(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0:
                    this.xPackageRoiPanelTop.Attach();
                    this.xPackageRoiPanelBottom.Attach();
                    this.xPackageRoiPanelLeft.Attach();
                    this.xPackageRoiPanelRight.Attach();
                    break;
                case 1:
                    this.xFirstPinRoiPanel.Attach();
                    this.xPatternRoiDataListPanel.AttachAll();
                    break;
                case 2:
                    this.xBallRoiDataListPanel.AttachAll();
                    break;
                case 3:
                    this.xSurfaceRoiDataListPanel.AttachAll();
                    break;
                case 4:
                    this.displaySelectedPoints(xSawOffsetListPanel.SawOffsetItems
                       .Select(e => e.SawOffsetTargetPoint)
                       .ToList());
                    break;
                case 5:
                    this.xRejectMarkRoiPanel.Attach();
                    break;
                case 6:
                    this.xDontCareRoiDataListPanel.AttachAll();
                    break;
            }
        }
        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.xBgaTeachingTabControl.SelectedIndex <= 0)
            {
                return;
            }

            this.xBgaTeachingTabControl.SelectedIndex--;
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.xBgaTeachingTabControl.SelectedIndex >= this.xBgaTeachingTabControl.Items.Count - 1)
            {
                return;
            }

            this.xBgaTeachingTabControl.SelectedIndex++;
        }

        private void finishButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            Close();
        }

        private void bgaTeachingTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 모든 TabControl에서의 Selection Event는 이 이벤트 핸들러로 propagated됩니다. 
            // 탭 전환이 아닌경우 얼리 리턴 합니다.
            if (e.OriginalSource != this.xBgaTeachingTabControl)
            {
                return;
            }

            if (!this.xVisionWindow.IsWindowLoaded)
            {
                return;
            }

            clearImage();

            this.xPackageRoiPanelTop.Detach();
            this.xPackageRoiPanelBottom.Detach();
            this.xPackageRoiPanelLeft.Detach();
            this.xPackageRoiPanelRight.Detach();
            this.xFirstPinRoiPanel.Detach();
            this.xPatternRoiDataListPanel.DetachAll();
            this.xBallRoiDataListPanel.DetachAll();
            this.xSurfaceRoiDataListPanel.DetachAll();
            this.xDontCareRoiDataListPanel.DetachAll();
            this.xRejectMarkRoiPanel.Detach();
            this.xPackageModelRoiPanel.Detach();

            attachDrawingObjectOfCurrentTab(this.xBgaTeachingTabControl.SelectedIndex);
        }

        private void clearImage()
        {
            this.xVisionWindow.Clear();
            this.xVisionWindow.Display(this.teachingImage!);
            this.xVisionWindow.SetFullImagePart();
        }

        private void packageAutoRoiButton_OnClick(object sender, RoutedEventArgs e)
        {
            clearImage();
            this.xPackageRoiPanelTop.CreateRoi();
            this.xPackageRoiPanelBottom.CreateRoi();
            this.xPackageRoiPanelLeft.CreateRoi();
            this.xPackageRoiPanelRight.CreateRoi();
        }

        private void findPackageButton_OnClick(object sender, RoutedEventArgs e)
        {
            clearImage();
            this.xPackageRoiPanelTop.Detach();
            this.xPackageRoiPanelBottom.Detach();
            this.xPackageRoiPanelLeft.Detach();
            this.xPackageRoiPanelRight.Detach();
        }

        private void findFistPinAndPatternButton_OnClick(object sender, RoutedEventArgs e)
        {
            clearImage();
            this.xFirstPinRoiPanel.Detach();
        }

        private void findBallButton_OnClick(object sender, RoutedEventArgs e)
        {
            clearImage();
        }

        //private void xDontCareRoiDataListPanel_Loaded(object sender, RoutedEventArgs e)
        //{

        //}
    }
}