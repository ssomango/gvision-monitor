using GVisionWpf.DomainLayer.Data.Inspection.Item;

namespace GVisionWpf.GlobalStates
{
    public class Inspection
    {
        public HashSet<MoldInspectionItem> MoldItems;
        public HashSet<BgaInspectionItem> BgaItems;
        public HashSet<QfnInspectionItem> QfnItems;
        public HashSet<LgaInspectionItem> LgaItems;

        public Dictionary<EResultType, EColor> MapColors;
        public Dictionary<EResultType, EColor> BgaColors;
        public Dictionary<EResultType, EColor> QfnColors;
        public Dictionary<EResultType, EColor> LgaColors;

        public Tolerance Tolerance;
        public Double BallDiameters;
        public Double BallPitch;

        public double BgaSawOffsetXStandard;
        public double BgaSawOffsetYStandard;
        public ESaveOption SaveOption;
        public int SaveDays;
        public int DBSaveDays;
        public EInspectionMode Mode;
        public LengthUnit LengthUnit;
    }
}
