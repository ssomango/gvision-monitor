using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.Models.Visions;
using GVisionWpf.Types;
using System.ComponentModel.DataAnnotations;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public partial class CommonCalibrationTeaching : ObservableValidator
    {

        [ObservableProperty]
        private Roi roi = new Roi("ROI", 0, 0, 2040, 2048);

        [ObservableProperty]
        private Threshold threshold = new Threshold(0, 125);

        [ObservableProperty]
        private double similarity = 70;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 99999999)]
        private int minSize = 10;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 99999999)]
        private int maxSize = 999999;

        [ObservableProperty]
        private EShape shapeType = EShape.Circle;

        [ObservableProperty]
        private ECalibrationStandard standardType = ECalibrationStandard.Biggest;
    }
}