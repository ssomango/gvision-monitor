using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.UIs.Overlays;
using GVisionWpf.UIs.ViewModels.Teaching;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace GVisionWpf.UIs.Frames.Windows.Teaching
{
    /// <summary>
    /// GridMoldTeachingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GridMoldTeachingWindow : Window
    {
        private GridMoldTeachingViewModel viewModel;
        public Guid? MappingTutorGateToken { get; set; }

        #region Property

        public HObject? TeachingImage
        {
            get => this.viewModel.TeachingImage;
            set
            {
                this.viewModel.TeachingImage = value;
                CurrentTeachingWindow.Instance.TeachingImage = value;
            }
        }

        #endregion

        public GridMoldTeachingWindow()
        {
            InitializeComponent();
            registerEvents();

            xMarkRoiDataListPanel.ThresholdPanel = this.xMarkThresholdPanel;

            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;
            CurrentTeachingWindow.Instance.InspectionType = EInspection.Mapping;

            Loaded += onLoaded;
            Closed += OnClosed;  // aml

            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;
            this.viewModel = (GridMoldTeachingViewModel)this.DataContext;
        }
        // aml
        // ~GridMoldTeachingWindow() => unRegisterEVents();
        // aml
        private void OnClosed(object? sender, EventArgs e)
        {
            GridMoldTeachingFlow.StopForWindow(this, "WindowClosed");
            unRegisterEVents(); // 여기서 안전하게 해제
        }

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

            HObject pointRegion = point.GenReticle();
            using (pointRegion)
            {
                CurrentTeachingWindow.Instance.Window?.Display(pointRegion, EColor.Green);
            }
        }

        public GridMoldTeachingWindow(ObservableCollection<HObject> shots) : this()
        {
            this.viewModel.Shots = shots;

            foreach (HObject shot in shots)
            {
                HOperatorSet.CopyImage(shot, out HObject copiedShot);
                this.viewModel.OriginalImages.Add(copiedShot);
            }

            if (this.viewModel.SelectedShotIndex != -1)
            {
                TeachingImage = this.viewModel.Shots[this.viewModel.SelectedShotIndex];
            }
        }
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

        // angle을 텍스트로 직접 입력할 때
        //private void rotateAngle_onTextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if (!double.TryParse(this.rotateAngleTextBox.Text, out double angle))
        //    {
        //        this.rotateAngleTextBox.Text = "0.0";
        //        angle = 0.0;
        //    }
        //}

        private void tabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 모든 TabControl에서의 Selection Event는 이 이벤트 핸들러로 propagated됩니다. 
            // 탭 전환이 아닌경우 얼리 리턴 합니다.
            if (e.OriginalSource != this.xTabControl)
            {
                return;
            }

            if (!this.xVisionWindow.IsWindowLoaded)
            {
                return;
            }

            if (this.viewModel.SelectedPackageNo <= 0 && this.xTabControl.SelectedIndex != 0 && this.xTabControl.SelectedIndex != this.xTabControl.Items.Count - 1) 
            {
                new AlertWindow("The Package Required",
                    AlertWindow.EIcon.ALERT,
                    "The package outline must be found to move next steps.",
                    AlertWindow.EAlert.IMAGE,
                    "/Assets/UiImages/find-mark-guide.png").ShowDialog();
                return;
            }

            this.xGridRoiPanel.Detach();
            this.xMarkRoiDataListPanel.DetachAll();
            this.xCodeRoiPanel.Detach();
            this.xSurfaceRoiDataListPanel.DetachAll();
            this.xDontCareRoiDataListPanel.DetachAll();
            this.xRejectMarkRoiPanel.Detach();


            switch (this.xTabControl.SelectedIndex)
            {
                case 0:
                    this.xGridRoiPanel.Attach();
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
                    this.viewModel.ShowTeachingRegion();
                    break;
                case 5:
                    this.xRejectMarkRoiPanel.Attach();
                    break;
            }
        }

        private void findPackageButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.xGridRoiPanel.Detach();
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
            this.viewModel.RotateShotsAndTeachingImage(this.viewModel.RotateAngle);
            this.viewModel.VisionWindow = xVisionWindow;
            clearImage();
            GridMoldTeachingFlow.StartMappingTutor(this, this.viewModel, MappingTutorGateToken);
        }
    }
}
