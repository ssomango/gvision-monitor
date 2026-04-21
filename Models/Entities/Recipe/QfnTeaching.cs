using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GVisionWpf.Models.Entities.Recipe
{
    public sealed partial class QfnTeaching : InspectionTeaching { }

    partial class QfnTeaching : ISinglePackageTeachingModel<QfnTeaching>
    {
        #region Properties
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
        private Threshold packageThreshold = new Threshold(0, 120); // LEGACY, only for teaching

        [ObservableProperty]
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

    partial class QfnTeaching : IFirstPinTeachingModel<QfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        private EFirstPin firstPinType;

        [ObservableProperty]
        private Roi? firstPinRoi;

        [ObservableProperty]
        private Threshold firstPinThreshold = new Threshold(50, 255);

        [IgnoreDataMember]
        public Rect FirstPinRect { get; set; } = new Rect();

        #endregion
    }

    partial class QfnTeaching : IRejectMarkTeachingModel<QfnTeaching>
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

    partial class QfnTeaching : ISinglePadTeachingModel<QfnTeaching>
    {
        [ObservableProperty]
        private Size padSize = new Size(0, 0);

        [ObservableProperty]
        private Threshold padThreshold = new Threshold(50, 255);

        [ObservableProperty]
        private Roi? padRoi;

        [ObservableProperty]
        private int padArea = 0;
    }

    partial class QfnTeaching : ILeadTeachingModel<QfnTeaching>
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

    partial class QfnTeaching : ISawingTeachingModel<QfnTeaching>
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

    partial class QfnTeaching : IScratchTeachingModel<QfnTeaching>
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

    partial class QfnTeaching : IForeignMaterialTeachingModel<QfnTeaching>
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

    partial class QfnTeaching : IContaminationTeachingModel<QfnTeaching>
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

    partial class QfnTeaching : IDontCareTeachingModel<QfnTeaching>
    {
        #region Properties
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }
}