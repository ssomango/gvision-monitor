using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace GVisionWpf.Models.Entities.Recipe
{
    public partial class BgaTeaching : InspectionTeaching
    {
       
    }

    public partial class BgaTeaching : ISinglePackageTeachingModel<BgaTeaching>
    {
        #region Package Property

        [ObservableProperty]
        private Pose packageCenter;

        [ObservableProperty]
        private Roi? packageRoiTop;

        [ObservableProperty]
        private Roi? packageRoiBottom;

        [ObservableProperty]
        private Roi? packageRoiLeft;

        [ObservableProperty]
        private Roi? packageRoiRight;

        [ObservableProperty]
        private Threshold packageThreshold = new Threshold(0, 40); // LEGACY

        [ObservableProperty]
        private int packageThresholdDiff;

        [ObservableProperty]
        private EEdgeDetectDirection packageEdgeDetectDirection;

        [ObservableProperty]
        private EEdgeDetectMode packageEdgeDetectMode;

        [ObservableProperty]
        private HTuple? modelHandleForAlign;

        [ObservableProperty]
        private HTuple? homMat2DModelForAlign;

        [ObservableProperty]
        private Roi? packageModelRoi;
        #endregion
    }

    public partial class BgaTeaching : IFirstPinTeachingModel<BgaTeaching>
    {
        #region FirstPin Property
        [ObservableProperty]
        private Roi? firstPinRoi;

        [ObservableProperty]
        private Threshold firstPinThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private EFirstPin firstPinType = EFirstPin.SmallPad;

        [ObservableProperty]
        private Rect firstPinRect;
        #endregion
    }

    public partial class BgaTeaching : IPatternTeachingModel<BgaTeaching>
    {
        #region Pattern Property
        [ObservableProperty]
        private ObservableCollection<Roi> patternRois = new ObservableCollection<Roi>();

        [ObservableProperty]
        private Threshold patternThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private List<Rect> patterns = new List<Rect>();
        #endregion
    }

    public partial class BgaTeaching : IBallTeachingModel<BgaTeaching>
    {
        #region Ball Property
        [ObservableProperty]
        private IList<Roi> ballRois = new ObservableCollection<Roi>();

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 255)]
        private Threshold ballThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private List<Circle> balls = new List<Circle>();

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private double ballMinArea = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private double ballMaxArea = 999999;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 100)]
        private int ballMinCircularity = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 100)]
        private double ballPositionOffset = 1;

        [ObservableProperty]
        private double ballMinSize = 0;

        [ObservableProperty]
        private double ballMaxSize = 0;

        [ObservableProperty]
        private double ballAvgDiameters = 0;

        [ObservableProperty]
        private Length ballAvgPitch = new Length();


        public Dictionary<string, List<Circle>> BallsByRoi { get; set; } = new();
        public Dictionary<string, double> BallDiametersByRoi { get; set; } = new();
        public Dictionary<string, Length> BallPitchesByRoi { get; set; } = new();



        #endregion
    }

    public partial class BgaTeaching : IScratchTeachingModel<BgaTeaching>
    {
        #region Scratch Property
        [ObservableProperty]
        private Threshold scratchThreshold = new Threshold(120, 255);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 500000)]
        private int scratchMinSize = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int scratchMaxSize = 999999;

        [ObservableProperty]
        private ObservableCollection<Roi> surfaceRois = new ObservableCollection<Roi>();
        #endregion
    }

    public partial class BgaTeaching : IForeignMaterialTeachingModel<BgaTeaching>
    {
        #region ForeignMaterial Property
        [ObservableProperty]
        private Threshold foreignMaterialThreshold = new Threshold(120, 255);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private int foreignMaterialMinSize = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int foreignMaterialMaxSize = 999999;
        #endregion
    }

    public partial class BgaTeaching : IContaminationTeachingModel<BgaTeaching>
    {
        #region Contamination Property
        [ObservableProperty]
        private Threshold contaminationThreshold = new Threshold(0, 120);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private int contaminationMinSize = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int contaminationMaxSize = 999999;
        #endregion
    }

    public partial class BgaTeaching : ISawingTeachingModel<BgaTeaching>
    {
        #region Sawing Property

        [ObservableProperty]
        private ICollection<SawOffsetItem> sawOffsetItems = new ObservableCollection<SawOffsetItem>();

        [ObservableProperty]
        private double outlineWidth = 300;

        [ObservableProperty]
        private Threshold outlineThreshold = new Threshold(0, 50);

        [ObservableProperty]
        private double minLengthOfShortSide, maxLengthOfShortSide;

        [ObservableProperty]
        private double minLengthOfLongSide, maxLengthOfLongSide;
        #endregion
    }

    public partial class BgaTeaching : IRejectMarkTeachingModel<BgaTeaching>
    {
        #region RejectMark Property
        [ObservableProperty]
        private Roi? rejectMarkRoi;

        [ObservableProperty]
        private Threshold rejectMarkThreshold = new Threshold(120, 255);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private int rejectMarkMinSize = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int rejectMarkMaxSize = 999999;
        #endregion
    }

    public partial class BgaTeaching : IDontCareTeachingModel<BgaTeaching>
    {
        #region DontCare Property
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }
}