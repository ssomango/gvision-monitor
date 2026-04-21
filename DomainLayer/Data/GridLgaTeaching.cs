using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using Newtonsoft.Json;

namespace GVisionWpf.DomainLayer.Data
{
    public partial class GridLgaTeaching : InspectionTeaching
    {
        [JsonIgnore]
        public Dictionary<InspectionItem, int> ShotNumberForInspection { get; set; } = new Dictionary<InspectionItem, int>();

        [JsonProperty("ShotNumberForInspection")]
        public Dictionary<string, int> ShotNumberForInspectionRaw
        {
            get => ShotNumberForInspection.ToDictionary(kv => kv.Key.Name, kv => kv.Value);
            set => ShotNumberForInspection = value.ToDictionary(kv => LgaInspectionItem.FromName(kv.Key), kv => kv.Value);
        }
    }

    partial class GridLgaTeaching : IGridPackageTeachingModel<GridLgaTeaching>
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

    partial class GridLgaTeaching : IFirstPinTeachingModel<GridLgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private EFirstPin firstPinType;

        [ObservableProperty]
        private Roi firstPinRoi = new Roi("FIRST PIN");

        [ObservableProperty]
        private Threshold firstPinThreshold = new Threshold(120, 255);

        [IgnoreDataMember]
        public Rect FirstPinRect { get => throw new NotImplementedException(); set; }
        #endregion
    }

    partial class GridLgaTeaching : ILeadTeachingModel<GridLgaTeaching>
    {
        #region Properties

        [ObservableProperty]
        private ObservableCollection<Roi> leadRois = new ObservableCollection<Roi>();

        [ObservableProperty]
        private Threshold leadThreshold = new Threshold(40, 255);

        [ObservableProperty]
        private IEnumerable<Pose> leadPxPoses = new List<Pose>();

        [ObservableProperty]
        private int leadAverageArea = 0;

        [ObservableProperty]
        private double leadAveragePerimeter = 0;

        [ObservableProperty]
        private double leadAveragePitch = 0;

        [ObservableProperty]
        private Size leadAverageSize = new Size(0, 0);

        [ObservableProperty]
        private int leadContaminationMinSize = 100;

        [ObservableProperty]
        private int leadContaminationMaxSize = 999999;

        #endregion
    }

    partial class GridLgaTeaching : IMultiPadTeachingModel<GridLgaTeaching>
    {
        #region Properties

        [ObservableProperty]
        private ObservableCollection<Roi> padRois = new ObservableCollection<Roi>();

        [ObservableProperty]
        private Threshold multiPadThreshold = new Threshold(50, 255);

        [ObservableProperty]
        private StatisticalList<Size> multiPadSizes = new StatisticalList<Size>();

        [ObservableProperty]
        private Size multiPadAvgSize = new Size(0, 0);

        [ObservableProperty]
        private IEnumerable<Pose> padPxPoses = new List<Pose>();

        [ObservableProperty]
        private int multiPadAvgArea = 0;

        [ObservableProperty]
        private double multiPadAvgPitch = 0;

        [ObservableProperty]
        private double multiPadAvgPerimeter = 0;

        [ObservableProperty]
        private int padContaminationMinSize = 100;

        [ObservableProperty]
        private int padContaminationMaxSize = 999999;

        #endregion
    }

    partial class GridLgaTeaching : ISawingTeachingModel<GridLgaTeaching>
    {
        #region Properties

        [ObservableProperty]
        private ICollection<SawOffsetItem> sawOffsetItems = new ObservableCollection<SawOffsetItem>();

        [ObservableProperty]
        private double outlineWidth = 300;

        [ObservableProperty]
        private Threshold outlineThreshold = new Threshold(0, 50);

        [ObservableProperty]
        private double minLengthOfShortSide;

        [ObservableProperty]
        private double maxLengthOfShortSide;

        [ObservableProperty]
        private double minLengthOfLongSide;

        [ObservableProperty]
        private double maxLengthOfLongSide;

        #endregion
    }

    partial class GridLgaTeaching : IRejectMarkTeachingModel<GridLgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private Roi rejectMarkRoi = new Roi("REJECT MARK");

        [ObservableProperty]
        private Threshold rejectMarkThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private int rejectMarkMinSize = 1;

        [ObservableProperty]
        private int rejectMarkMaxSize = 999999;
        #endregion
    }

    partial class GridLgaTeaching : IDontCareTeachingModel<GridLgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }

    partial class GridLgaTeaching : IScratchTeachingModel<GridLgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private Threshold scratchThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private int scratchMinSize = 1;

        [ObservableProperty]
        private int scratchMaxSize = 999999;

        [ObservableProperty]
        private ObservableCollection<Roi> surfaceRois = new ObservableCollection<Roi>();
        #endregion
    }

    partial class GridLgaTeaching : IForeignMaterialTeachingModel<GridLgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private Threshold foreignMaterialThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private int foreignMaterialMinSize = 1;

        [ObservableProperty]
        private int foreignMaterialMaxSize = 999999;
        #endregion
    }

    partial class GridLgaTeaching : IContaminationTeachingModel<GridLgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private Threshold contaminationThreshold = new Threshold(120, 255);

        [ObservableProperty]
        private int contaminationMinSize = 1;

        [ObservableProperty]
        private int contaminationMaxSize = 999999;
        #endregion
    }
}
