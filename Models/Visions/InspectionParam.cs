namespace GVisionWpf.Models.Visions
{
    public class InspectionParam
    {
        public int TableNo;
        public int XPositionInFOV;
        public int YPositionInFOV;
        public int XPositionForGrid;
        public int YPositionForGrid;
        public int PackageNoInFov;

        public bool IsXOut;

        public Roi PackageTopRoi;
        public Roi PackageBottomRoi;
        public Roi PackageLeftRoi;
        public Roi PackageRightRoi;
    }
}
