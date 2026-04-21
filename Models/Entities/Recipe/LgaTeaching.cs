using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GVisionWpf.Models.Entities.Recipe
{
    public partial class LgaTeaching : InspectionTeaching
    {
    }

    partial class LgaTeaching : ISinglePackageTeachingModel<LgaTeaching>
    {
        #region Properties

        [ObservableProperty]
        private Roi? packageRoiTop;

        [ObservableProperty]
        private Roi? packageRoiBottom;

        [ObservableProperty]
        private Roi? packageRoiLeft;

        [ObservableProperty]
        private Roi? packageRoiRight;

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

        [ObservableProperty]
        private HTuple? modelHandleForAlign;

        [ObservableProperty]
        private HTuple? homMat2DModelForAlign;

        [ObservableProperty]
        private Roi? packageModelRoi;
        #endregion
    }

    partial class LgaTeaching : IFirstPinTeachingModel<LgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private EFirstPin firstPinType;

        [ObservableProperty]
        private Roi? firstPinRoi = null;

        [ObservableProperty]
        private Threshold firstPinThreshold = new Threshold(120, 255);

        [IgnoreDataMember]
        public Rect FirstPinRect { get; set; } = new Rect();
        #endregion
    }

    partial class LgaTeaching : ILeadTeachingModel<LgaTeaching>
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

    partial class LgaTeaching : IMultiPadTeachingModel<LgaTeaching>
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
        [NotifyDataErrorInfo]
        [Range(1, 50000)]
        private int padContaminationMinSize = 100;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000000)]
        private int padContaminationMaxSize = 999999;

        #endregion
    }

    partial class LgaTeaching : ISawingTeachingModel<LgaTeaching>
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

    partial class LgaTeaching : IRejectMarkTeachingModel<LgaTeaching>
    {
        #region Properties
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

    partial class LgaTeaching : IDontCareTeachingModel<LgaTeaching>
    {
        #region Properties
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }

    partial class LgaTeaching : IScratchTeachingModel<LgaTeaching>
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

    partial class LgaTeaching : IForeignMaterialTeachingModel<LgaTeaching>
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

    partial class LgaTeaching : IContaminationTeachingModel<LgaTeaching>
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
}
