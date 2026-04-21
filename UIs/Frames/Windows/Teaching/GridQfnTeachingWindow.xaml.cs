using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.UIs.ViewModels.Teaching;

namespace GVisionWpf.UIs.Frames.Windows.Teaching
{
    /// <summary>
    /// GridQfnTeachingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GridQfnTeachingWindow : Window
    {
        private GridQfnTeachingViewModel viewModel;

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

        public GridQfnTeachingWindow()
        {
            InitializeComponent();
            registerEvents();

            Loaded += onLoaded;
            CurrentTeachingWindow.Instance.Window = this.xVisionWindow;
            CurrentTeachingWindow.Instance.InspectionType = EInspection.Bga;

            viewModel = (GridQfnTeachingViewModel)DataContext;
        }

        public GridQfnTeachingWindow(ObservableCollection<HObject> shots) : this()
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

        ~GridQfnTeachingWindow() => unRegisterEVents();

        #region Events
        private void registerEvents()
        {
            xSawOffsetListPanel.PointsUpdated += xPointPickPanel_PointSelected;
        }

        private void unRegisterEVents()
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

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            viewModel.VisionWindow = this.xVisionWindow;
            viewModel.TeachingImage = TeachingImage;
            clearImage();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != this.xTabControl) return;
            if (!this.xVisionWindow.IsWindowLoaded) return;

            clearImage();

            this.xGridRoiPanel.Detach();
            this.xFirstPinRoiPanel.Detach();
            this.xPadRoiPanel.Detach();
            this.xLeadRoisDataListPanel.DetachAll();
            this.xSurfaceRoiDataListPanel.DetachAll();
            this.xDontCareRoiDataListPanel.DetachAll();
            this.xRejectMarkRoiPanel.Detach();

            attachDrawingObjectOfCurrentTab(this.xTabControl.SelectedIndex);
        }

        private void attachDrawingObjectOfCurrentTab(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0:
                    this.xGridRoiPanel.Attach();
                    this.xFirstPinRoiPanel.Attach();
                    break;
                case 1:
                    this.xPadRoiPanel.Attach();
                    this.xLeadRoisDataListPanel.AttachAll();
                    break;
                case 2:
                    this.xSurfaceRoiDataListPanel.AttachAll();
                    break;
                case 3:
                    this.displaySelectedPoints(xSawOffsetListPanel.SawOffsetItems
                        .Select(e => e.SawOffsetTargetPoint)
                        .ToList());
                    this.viewModel.ShowTeachingRegion();
                    break;
                case 4:
                    this.xRejectMarkRoiPanel.Attach();
                    break;
                case 5:
                    this.xDontCareRoiDataListPanel.AttachAll();
                    break;
            }
        }

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

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            Close();
        }

        private void finishButton_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            Close();
        }

        private void findPackageButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.xGridRoiPanel.Detach();
            clearImage();
        }

        private void findPadAndLeadsBtn_OnClick(object sender, RoutedEventArgs e)
        {
            this.xPadRoiPanel.Detach();
            clearImage();
        }

        private void clearImage()
        {
            this.xVisionWindow.Clear();
            this.xVisionWindow.Display(TeachingImage);
            this.xVisionWindow.SetFullImagePart();
        }
    }
}
