using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using Newtonsoft.Json;

namespace GVisionWpf.DomainLayer.Data
{

    public sealed partial class GridQfnTeaching : InspectionTeaching 
    {
        [JsonIgnore]
        public Dictionary<InspectionItem, int> ShotNumberForInspection { get; set; } = new Dictionary<InspectionItem, int>();

        [JsonProperty("ShotNumberForInspection")]
        public Dictionary<string, int> ShotNumberForInspectionRaw
        {
            get => ShotNumberForInspection.ToDictionary(kv => kv.Key.Name, kv => kv.Value);
            set => ShotNumberForInspection = value.ToDictionary(kv => QfnInspectionItem.FromName(kv.Key), kv => kv.Value);
        }
    }


    partial class GridQfnTeaching : IGridPackageTeachingModel<GridQfnTeaching>
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

    partial class GridQfnTeaching : IFirstPinTeachingModel<GridQfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        private EFirstPin firstPinType;

        [ObservableProperty]
        private Roi firstPinRoi = new Roi("FIRST PIN", 100, 100, 500, 500);

        [ObservableProperty]
        private Threshold firstPinThreshold = new Threshold(50, 255);

        [IgnoreDataMember]
        public Rect FirstPinRect { get; set; } = new Rect();
        #endregion
    }

    partial class GridQfnTeaching : IRejectMarkTeachingModel<GridQfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        private Roi rejectMarkRoi = new Roi("REJECT MARK");

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

    partial class GridQfnTeaching : ISinglePadTeachingModel<GridQfnTeaching>
    {
        [ObservableProperty]
        private Size padSize = new Size(0, 0);

        [ObservableProperty]
        private Threshold padThreshold = new Threshold(50, 255);

        [ObservableProperty]
        private Roi padRoi = new Roi("PAD", 100, 100, 500, 500);

        [ObservableProperty]
        private int padArea = 0;
    }

    partial class GridQfnTeaching : ILeadTeachingModel<GridQfnTeaching>
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
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private int leadContaminationMinSize = 100;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int leadContaminationMaxSize = 999999;

        #endregion

    }

    partial class GridQfnTeaching : ISawingTeachingModel<GridQfnTeaching>
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

    partial class GridQfnTeaching : IScratchTeachingModel<GridQfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 255)]
        private Threshold scratchThreshold = new Threshold(120, 255);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private int scratchMinSize = 1;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int scratchMaxSize = 999999;

        [ObservableProperty]
        private ObservableCollection<Roi> surfaceRois = new ObservableCollection<Roi>();
        #endregion
    }

    partial class GridQfnTeaching : IForeignMaterialTeachingModel<GridQfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 255)]
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

    partial class GridQfnTeaching : IContaminationTeachingModel<GridQfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 255)]
        private Threshold contaminationThreshold = new Threshold(120, 255);

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

    partial class GridQfnTeaching : IDontCareTeachingModel<GridQfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }

}