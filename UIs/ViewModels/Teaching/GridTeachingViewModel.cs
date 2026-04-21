using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.UiModels;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.Visions;
using GVisionWpf.Visions.Engines;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public abstract partial class GridTeachingViewModelBase<T> : ViewModelBase
        where T : InspectionTeaching, IGridPackageTeachingModel<T>
    {

        protected HObject? teachingPackageRegion;
        protected List<Point> teachingPackagePoints;

        protected Dictionary<int, PackageInfo> packages = new Dictionary<int, PackageInfo>();

        public double RotateAngle
        {
            get => Teaching.RotateAngle;
            set
            {
                this.RotateShotsAndTeachingImage(-RotateAngle);
                this.RotateShotsAndTeachingImage(value);
                VisionWindow?.Display(TeachingImage);
                VisionWindow?.SetFullImagePart();

                Teaching.RotateAngle = value;
            }
        }

        #region Observable Properties

        [ObservableProperty]
        private T teaching;

        [ObservableProperty]
        private HObject teachingImage;

        [ObservableProperty]
        private VisionWindow? visionWindow;

        [ObservableProperty]
        private ObservableCollection<HObject> originalImages = new ObservableCollection<HObject>();

        [ObservableProperty]
        private ObservableCollection<HObject> shots;

        [ObservableProperty]
        private ObservableCollection<int> shotNumbers = new ObservableCollection<int>();

        [ObservableProperty]
        private int selectedShotIndex = -1;

        [ObservableProperty]
        private ObservableCollection<int> packageNumbers = new ObservableCollection<int>();

        [ObservableProperty]
        private int selectedPackageNo = -1;

        [ObservableProperty]
        private int tapControlSelectedIndex = 0;

        [ObservableProperty]
        private string inspectionResultStr;

        #endregion


        #region OnPropertyChanged
        partial void OnTeachingImageChanged(HObject? oldValue, HObject newValue)
        {
            CurrentTeachingWindow.Instance.TeachingImage = newValue;
        }

        partial void OnShotsChanged(ObservableCollection<HObject>? oldValue, ObservableCollection<HObject> newValue)
        {
            for (int shotNumber = 1; shotNumber <= newValue.Count; shotNumber++)
            {
                ShotNumbers.Add(shotNumber);
            }
        }

        partial void OnSelectedPackageNoChanged(int oldValue, int newValue)
        {
            if (oldValue == newValue) return;

            this.packages.TryGetValue(newValue, out PackageInfo packageInfo);
            this.teachingPackageRegion = packageInfo.PackageRegion;
            this.teachingPackagePoints = packageInfo.PackagePoints;

            VisionOperation.GetRegionOrientationOfSmallestRectangle2(packageInfo.PackageRegion, out Pose packagePose, out _);

            Teaching.SelectedPackageIndex = newValue;
            Teaching.PackageCenter = packagePose;
        }

        partial void OnSelectedShotIndexChanged(int oldValue, int newValue)
        {
            if (Shots == null) { return; }

            int tabIdx = TapControlSelectedIndex;
            int shotIndex = SelectedShotIndex;

            Teaching.ShotNoByTabNo[tabIdx] = shotIndex;
            SaveShotNumber();

            if (shotIndex < 0)
            {
                return;
            }

            TeachingImage = Shots[shotIndex];
            CurrentTeachingWindow.Instance.Window!.Display(TeachingImage);
            CurrentTeachingWindow.Instance.Window!.SetFullImagePart();
            ShowTeachingRegion();
        }

        partial void OnTapControlSelectedIndexChanged(int oldValue, int newValue)
        {
            int shotIndex;
            bool hasShotNo = Teaching.ShotNoByTabNo.TryGetValue(TapControlSelectedIndex, out shotIndex);

            shotIndex = hasShotNo ? shotIndex : -1;

            if (hasShotNo && shotIndex >= 0)
            {
                TeachingImage = Shots[shotIndex];
                VisionWindow?.Display(TeachingImage);
                ShowTeachingRegion();
            }
            else
            {
                VisionWindow?.Clear();
            }

            SelectedShotIndex = shotIndex;
        }
        #endregion

        public abstract void SaveShotNumber();

        public void RotateShotsAndTeachingImage(double angle)
        {
            if (TeachingImage != null && TeachingImage.Key != IntPtr.Zero)
            {
                TeachingImage = VisionOperation.RotateImage(TeachingImage, angle);
            }

            // Shots가 null이거나 비어있으면 그냥 리턴
            if (Shots == null || Shots.Count == 0)
                return;

            for (int i = 0; i < Shots.Count; i++)
            {
                Shots[i] = VisionOperation.RotateImage(Shots[i], angle);
            }
        }

        public void ClearImage()
        {
            VisionWindow?.Clear();
            VisionWindow?.Display(CurrentTeachingWindow.Instance.TeachingImage);
        }

        public void ShowTeachingRegion()
        {
            if (Teaching.PackageRoi == null || this.teachingPackageRegion == null)
            {
                ClearImage();
                return;
            }

            VisionWindow?.Display(this.teachingPackageRegion, "green");

            if (teachingPackageRegion != null) 
            {
                var selectedPackageRoi = teachingPackageRegion.Region2Roi();
                VisionEngine.GetTextOfPackageNumber(selectedPackageRoi, out FloatingText packageNumberText, Teaching.SelectedPackageIndex);
                VisionWindow?.Display(packageNumberText);
            }
        }

        protected virtual void ShowInspectionResultText(IEnumerable<IInspectionResultModel> results)
        {
            int nGoodDevices = results.Count(result => InspectionResultConverter.ErrorTypeInEResultType(result) == EResultType.Good);
            int nBadDevices = results.Count(result => InspectionResultConverter.ErrorTypeInEResultType(result) != EResultType.Good);

            FixedText totalText = new FixedText($"TOTAL: {nGoodDevices + nBadDevices}, GOOD: {nGoodDevices}, BAD: {nBadDevices}", 1, nBadDevices > 0 ? EColor.Red : EColor.Green);

            CurrentTeachingWindow.Instance?.Window?.Display(totalText);
        }
    }
}
