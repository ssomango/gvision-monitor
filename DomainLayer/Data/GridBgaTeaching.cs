using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using Newtonsoft.Json;

namespace GVisionWpf.DomainLayer.Data
{
    public partial class GridBgaTeaching : InspectionTeaching 
    {
        [JsonIgnore]
        public Dictionary<InspectionItem, int> ShotNumberForInspection { get; set; } = new Dictionary<InspectionItem, int>();

        [JsonProperty("ShotNumberForInspection")]
        public Dictionary<string, int> ShotNumberForInspectionRaw
        {
            get => ShotNumberForInspection.ToDictionary(kv => kv.Key.Name, kv => kv.Value);
            set => ShotNumberForInspection = value.ToDictionary(kv => BgaInspectionItem.FromName(kv.Key), kv => kv.Value);
        }
    }

    partial class GridBgaTeaching : IGridPackageTeachingModel<GridBgaTeaching>
    {
        #region Grid Package

        [ObservableProperty]
        private int selectedPackageIndex = 0;

        [ObservableProperty]
        private double rotateAngle = 0;

        [ObservableProperty]
        private int rowSize = 2;

        [ObservableProperty]
        private int columnSize = 2;

        [ObservableProperty]
        private Roi packageRoi = new Roi("PACKAGE GRID");

        [ObservableProperty]
        private Pose packageCenter;

        [ObservableProperty]
        private Threshold packageThreshold = new Threshold(0, 120);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 255)]
        private int packageThresholdDiff = 0;


        [ObservableProperty]
        private EEdgeDetectDirection packageEdgeDetectDirection = EEdgeDetectDirection.InToOut;

        [ObservableProperty]
        private EEdgeDetectMode packageEdgeDetectMode = EEdgeDetectMode.BlackToWhite;

        public Dictionary<int, int> ShotNoByTabNo { get; set; } = new Dictionary<int, int>()
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 },
        };

        #endregion
    }

    public partial class GridBgaTeaching : IFirstPinTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IPatternTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IBallTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IScratchTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IForeignMaterialTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IContaminationTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : ISawingTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IRejectMarkTeachingModel<GridBgaTeaching>
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

    public partial class GridBgaTeaching : IDontCareTeachingModel<GridBgaTeaching>
    {
        #region DontCare Property
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }
}