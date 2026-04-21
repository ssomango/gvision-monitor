using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;


namespace GVisionWpf.Models.Entities.Recipe
{
    public partial class MoldTeaching : InspectionTeaching
    {
        public double RotateAngle = 0;
    }

    partial class MoldTeaching : ISinglePackageTeachingModel<MoldTeaching>
    {
        #region Properties
        [ObservableProperty]
        private Pose packageCenter;

        [ObservableProperty]
        private Roi packageRoiTop = new Roi("TOP", 100, 100, 500, 500);

        [ObservableProperty]
        private Roi packageRoiBottom = new Roi("BOTTOM", 100, 100, 500, 500);

        [ObservableProperty]
        private Roi packageRoiLeft = new Roi("LEFT", 100, 100, 500, 500);

        [ObservableProperty]
        private Roi packageRoiRight = new Roi("RIGHT", 100, 100, 500, 500);

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
        private Roi packageModelRoi = new Roi("MODEL");
        #endregion
    }

    partial class MoldTeaching : IDataCodeTeachingModel<MoldTeaching>
    {
        #region Code
        [ObservableProperty]
        private Roi codeRoi = new Roi("CODE");
        #endregion
    }

    partial class MoldTeaching : IMarkTeachingModel<MoldTeaching>
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

    partial class MoldTeaching : ISawingTeachingModel<MoldTeaching>
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

    partial class MoldTeaching : IForeignMaterialTeachingModel<MoldTeaching>
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

    partial class MoldTeaching : IContaminationTeachingModel<MoldTeaching>
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

    partial class MoldTeaching : IScratchTeachingModel<MoldTeaching>
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

    partial class MoldTeaching : IRejectMarkTeachingModel<MoldTeaching>
    {
        #region Reject Mark
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

    partial class MoldTeaching : IDontCareTeachingModel<MoldTeaching>
    {
        #region Dontcare
        [ObservableProperty]
        private ObservableCollection<Roi> dontCareRois = new ObservableCollection<Roi>();
        #endregion
    }
}