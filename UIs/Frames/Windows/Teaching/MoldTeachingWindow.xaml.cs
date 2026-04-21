using System.Windows;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.UIs.ViewModels.Teaching;

namespace GVisionWpf.UIs.Frames.Windows.Teaching
{
    /// <summary>
    /// MoldTeachingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MoldTeachingWindow : Window
    {
        private HObject? teachingImage;

        private MoldTeachingViewModel viewModel;

        #region Property

        public HObject? TeachingImage
        {
            get => viewModel.TeachingImage;
            set
            {
                viewModel.TeachingImage = value;
                this.teachingImage = value;
                CurrentTeachingWindow.Instance.TeachingImage = value;
            }
        }

        #endregion
        public MoldTeachingWindow()
        {
            InitializeComponent();
            registerEvents();
            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;
            CurrentTeachingWindow.Instance.InspectionType = EInspection.Bga;

            viewModel = (MoldTeachingViewModel)DataContext;
            Loaded += onLoaded;
        }

        ~MoldTeachingWindow() => unRegisterEVents();

        #region Events
        private void registerEvents()
        {
            xSawOffsetPanel.PointSelected += xPointPickPanel_PointSelected;
        }

        private void unRegisterEVents()
        {
            xSawOffsetPanel.PointSelected -= xPointPickPanel_PointSelected;
        }

        private void xPointPickPanel_PointSelected(object? sender, EventArgs e)
        {
            if (e is ItemEventArgs<Models.Visions.Point> { Item: var point })
            {
                displaySelectedPoint(point);
            }
        }

        #endregion

        private void displaySelectedPoint(Models.Visions.Point? point)
        {
            if (point == null) return;

            clearImage();

            using HObject pointRegion = point.GenReticle();
            viewModel.VisionWindow?.Display(pointRegion, EColor.Green);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.xTabControl.SelectedIndex <= 0) return;
            this.xTabControl.SelectedIndex--;
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.xTabControl.SelectedIndex >= this.xTabControl.Items.Count - 1) return;
            this.xTabControl.SelectedIndex++;
        }

        private void finishButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            Close();
        }

        private void clearImage()
        {
            this.xVisionWindow.Clear();
            this.xVisionWindow.Display(TeachingImage);
            this.xVisionWindow.SetFullImagePart();
        }

        private void tabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 모든 TabControl에서의 Selection Event는 이 이벤트 핸들러로 propagated됩니다. 
            // 탭 전환이 아닌경우 얼리 리턴 합니다.
            if (e.OriginalSource != this.xTabControl) return;

            if (!this.xVisionWindow.IsWindowLoaded) return;

            this.xPackageRoiPanelTop.Detach();
            this.xPackageRoiPanelBottom.Detach();
            this.xPackageRoiPanelLeft.Detach();
            this.xPackageRoiPanelRight.Detach();

            this.xMarkRoiDataListPanel.DetachAll();
            this.xCodeRoiPanel.Detach();

            this.xSurfaceRoiDataListPanel.DetachAll();
            this.xDontCareRoiDataListPanel.DetachAll();

            this.xRejectMarkRoiPanel.Detach();

            switch (this.xTabControl.SelectedIndex)
            {
                case 0:
                    this.xPackageRoiPanelTop.Detach();
                    this.xPackageRoiPanelBottom.Detach();
                    this.xPackageRoiPanelLeft.Detach();
                    this.xPackageRoiPanelRight.Detach();
                    break;
                case 1:
                    this.xMarkRoiDataListPanel.AttachAll();
                    this.xCodeRoiPanel.Attach();
                    break;
                case 2:
                    this.xDontCareRoiDataListPanel.AttachAll();
                    break;
                case 3:
                    this.xSurfaceRoiDataListPanel.AttachAll();
                    break;
                case 4:
                    this.displaySelectedPoint(xSawOffsetPanel.xPointPickPanel.Point);
                    break;
                case 5:
                    this.xRejectMarkRoiPanel.Attach();
                    break;
            }
        }

        private void findPackageButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.xPackageRoiPanelTop.Detach();
            this.xPackageRoiPanelBottom.Detach();
            this.xPackageRoiPanelLeft.Detach();
            this.xPackageRoiPanelRight.Detach();
            clearImage();
        }

        private void findMarkButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.xMarkRoiDataListPanel.DetachAll();
            this.xCodeRoiPanel.Detach();
        }

        private void findSurfaceButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.xSurfaceRoiDataListPanel.DetachAll();
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            this.viewModel.VisionWindow = xVisionWindow;
            clearImage();
        }

        private void packageAutoRoiButton_OnClick(object sender, RoutedEventArgs e)
        {
            clearImage();
            this.xPackageRoiPanelTop.CreateRoi();
            this.xPackageRoiPanelBottom.CreateRoi();
            this.xPackageRoiPanelLeft.CreateRoi();
            this.xPackageRoiPanelRight.CreateRoi();
        }
    }
}
