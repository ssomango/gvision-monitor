using GVisionWpf.Models.UiModels;

namespace GVisionWpf.Models.Entities.Recipe
{
    public class Device
    {
        public EInspection MapPackageType;
        public EInspection PrsPackageType;
        public TableLayout TraySize; // TODO: 이거 VisionTable로 이름 바꿔야함.
        public TableLayout FovSize;
        public TableLayout BlockSize;
        public Size PackageSize;
        public bool IsPrsUsed;
        public bool IsMappingUsed;
        public bool IsBarcodeUsed;

        public Device()
        {
            this.MapPackageType = EInspection.Mark;
            this.PrsPackageType = EInspection.Bga;
            this.TraySize = new TableLayout(10, 10);
            this.FovSize = new TableLayout(10, 10);
            this.BlockSize = new TableLayout(10, 10);
            this.PackageSize = new Size(10.01, 10.01);
            this.IsPrsUsed = true;
            this.IsMappingUsed = true;
            this.IsBarcodeUsed = false;
        }
    }
}
