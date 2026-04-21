using GVisionWpf.Repositories;
using GVisionWpf.Visions;

namespace GVisionWpf.UIs.UiUpdaters
{
    public class LiveMappingGridRoiProcessor : ILiveFrameProcessor
    {
        private readonly GridMoldRepository mapRepository = GridMoldRepository.Instance;

        public LiveMappingGridRoiProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
        {
            this.CameraType = cameraType;
            this.HSmartWindowControlWpf = hSmartWindowControlWpf;
        }

        private HObject getGridRoi()
        {
            GridMoldTeaching teaching = this.mapRepository.GetRecipe();
            VisionOperation.PartitionRectangle(teaching.PackageRoi, teaching.RowSize, teaching.ColumnSize, out HObject gridRoiRegion);
            return gridRoiRegion;
        }

        public override void Display(HObject image)
        {
            HObject gridRoi = getGridRoi();
            this.HSmartWindowControlWpf.HalconWindow.DispObj(image);
            this.HSmartWindowControlWpf.HalconWindow.SetDraw("margin");
            this.HSmartWindowControlWpf.HalconWindow.SetColor("green");
            this.HSmartWindowControlWpf.HalconWindow.SetLineWidth(2);
            this.HSmartWindowControlWpf.HalconWindow.DispObj(gridRoi);
            gridRoi.Dispose();
        }

        public override void SetCameraType(ECamera type)
        {
            this.CameraType = type;
            // CameraManager.Instance.Cameras[type].SetFreeRunning();
        }
    }
}