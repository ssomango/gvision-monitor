using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace GVisionWpf.Models.Entities.Recipe
{
    public partial class GridMoldTeaching : InspectionTeaching
    {
        [JsonIgnore]
        public Dictionary<InspectionItem, int> ShotNumberForInspection { get; set; } = new Dictionary<InspectionItem, int>();

        [JsonProperty("ShotNumberForInspection")]
        public Dictionary<string, int> ShotNumberForInspectionRaw
        {
            get => ShotNumberForInspection.ToDictionary(kv => kv.Key.Name, kv => kv.Value);
            set => ShotNumberForInspection = value.ToDictionary(kv => MoldInspectionItem.FromName(kv.Key), kv => kv.Value);
        }
    }

    partial class GridMoldTeaching : IGridPackageTeachingModel<GridMoldTeaching>
    {
        #region Grid Package

        [ObservableProperty]
        private int selectedPackageIndex = 0;

        [ObservableProperty]
        private int rowSize = 2;

        [ObservableProperty]
        private int columnSize = 2;

        [ObservableProperty]
        private double rotateAngle = 0;

        [ObservableProperty]
        private Pose packageCenter;

        [ObservableProperty]
        private Roi? packageRoi;

        [ObservableProperty]
        private EEdgeDetectDirection packageEdgeDetectDirection = EEdgeDetectDirection.OutToIn;

        [ObservableProperty]
        private EEdgeDetectMode packageEdgeDetectMode = EEdgeDetectMode.BlackToWhite;

        [ObservableProperty]
        private Threshold packageThreshold = new Threshold(0, 40);

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 255)]
        private int packageThresholdDiff;

        [ObservableProperty]
        private Dictionary<int, int> shotNoByTabNo = new Dictionary<int, int>()
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

    partial class GridMoldTeaching : IDataCodeTeachingModel<GridMoldTeaching>
    {
        #region Code
        [ObservableProperty]
        private Roi? codeRoi;
        #endregion
    }

    partial class GridMoldTeaching : IMarkTeachingModel<GridMoldTeaching>
    {
        #region Mark
        [ObservableProperty]
        private Threshold markThreshold = new Threshold(220, 255);

        [ObservableProperty]
        private List<MarkItem> markItems = new List<MarkItem>(8);

        [ObservableProperty]
        private Pose textOffset = new Pose();
        #endregion
    }

    partial class GridMoldTeaching : ISawingTeachingModel<GridMoldTeaching>
    {
        #region SawOffset

        [ObservableProperty]
        private ICollection<SawOffsetItem> sawOffsetItems = [];

        [ObservableProperty]
        private double outlineWidth = 300;

        [ObservableProperty]
        private Threshold outlineThreshold = new Threshold(180, 255);
        #endregion

        #region LengthOfShortSide
        [ObservableProperty]
        private double minLengthOfShortSide, maxLengthOfShortSide;
        #endregion

        #region LenghOfLongSide
        [ObservableProperty]
        private double minLengthOfLongSide, maxLengthOfLongSide;
        #endregion
    }

    partial class GridMoldTeaching : IForeignMaterialTeachingModel<GridMoldTeaching>
    {
        #region Foreign Material
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

    partial class GridMoldTeaching : IContaminationTeachingModel<GridMoldTeaching>
    {
        #region Contamination
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

    partial class GridMoldTeaching : IScratchTeachingModel<GridMoldTeaching>
    {
        #region Screatch
        [ObservableProperty]
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

    partial class GridMoldTeaching : IRejectMarkTeachingModel<GridMoldTeaching>
    {
        #region Reject Mark
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

    partial class GridMoldTeaching : IDontCareTeachingModel<GridMoldTeaching>
    {
        #region Dontcare
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }
}