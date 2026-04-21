using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.Types;
using GVisionWpf.UIs.Frames.Panels;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public abstract partial class CalibrationTabViewModel : ViewModelBase
    {
        public ECalibration CalibrationType { get; }
        public string TabName { get; }

        [ObservableProperty]
        protected CommonCalibrationTeaching teaching;
        //aml
        public RoiPanelV2? RoiPanel { get; set; }
        //aml
        public ThresholdControllerPanel? ThresholdPanel { get; set; }

        protected CalibrationTabViewModel(ECalibration type, string name)
        {
            CalibrationType = type;
            TabName = name;

            GetTeaching();

            if (Teaching != null)
            {
                Teaching.PropertyChanged += OnAnyPropertyChanged;
                Teaching.Roi ??= new Roi("ROI", 10, 10, 2040, 2048);
                Teaching.Threshold ??= new Threshold(0, 125);
            }
            else
            { 
                Teaching.Roi.PropertyChanged += OnAnyPropertyChanged;
                Teaching.Threshold.PropertyChanged += OnAnyPropertyChanged;
            }
        }

        protected abstract void GetTeaching();

        private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveRecipe();
        }

        protected abstract void SaveRecipe();
        public abstract Task ExecuteCalibration();

        protected static uint FindCameraId(ECamera targetCamera)
        {
            foreach ((uint cameraNo, ECamera cameraType) in GlobalSetting.Instance.ECameraNos)
            {
                if (cameraType == targetCamera)
                {
                    return cameraNo;
                }
            }

            throw new NotSupportedCameraException();
        }
    }
}