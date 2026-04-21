using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching.Package;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
namespace GVisionWpf.UIs.UiUpdaters
{
    public class LivePrsOffsetProcessor : ILiveFrameProcessor
    {
        private int index = 0;

        public LivePrsOffsetProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
        {
            this.CameraType = cameraType;
            this.HSmartWindowControlWpf = hSmartWindowControlWpf;
        }

        private IPackageInspectionResultModel<InspectionResult> inspect(HObject image, out AlignContext alignContext, out InspectionRenderData render)
        {
            var camera = ECamera.PRS;

            switch (DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType)
            {
                case EInspection.Bga:
                    BgaTeaching bgaTeaching = BgaRepository.Instance.GetRecipe();
                    return (InspectionResult)new SinglePackageTeachingService<BgaTeaching, BgaInspectionResult, BgaInspectionItem>()
                        .InspectPackage(image, bgaTeaching, camera, false, [BgaInspectionItem.PackageOffset], out alignContext, out render);
               
                case EInspection.Qfn:
                    QfnTeaching qfnTeaching = QfnRepository.Instance.GetRecipe();
                    return (InspectionResult)new SinglePackageTeachingService<QfnTeaching, QfnInspectionResult, QfnInspectionItem>()
                        .InspectPackage(image, qfnTeaching, camera, false, [QfnInspectionItem.PackageOffset], out alignContext, out render);
                   
                case EInspection.Lga:
                    LgaTeaching lgaTeaching = LgaRepository.Instance.GetRecipe();
                    return (InspectionResult)new SinglePackageTeachingService<LgaTeaching, LgaInspectionResult, LgaInspectionItem>()
                        .InspectPackage(image, lgaTeaching, camera, false, [LgaInspectionItem.PackageOffset], out alignContext, out render);
                  
                case EInspection.Mark:
                    MoldTeaching moldTeaching = MoldRepository.Instance.GetRecipe();
                    return (InspectionResult)new SinglePackageTeachingService<MoldTeaching, MapInspectionResult, MoldInspectionItem>()
                        .InspectPackage(image, moldTeaching, camera, false, [MoldInspectionItem.PackageOffset], out alignContext, out render);

                default:
                    throw new Exception("not prs type");
            }
        }

        public override void Display(HObject image)
        {
            if (this.index++ % 5 != 0)
            {
                return;
            }

            using var packageResult = inspect(image, out AlignContext alignContext, out InspectionRenderData render);
            bool isFound = packageResult.HasDevice.Value;
            Pose offset = packageResult.PackageOffset.Value;

            this.HSmartWindowControlWpf.HalconWindow.DispObj(image);
            this.HSmartWindowControlWpf.HalconWindow.SetDraw("margin");

            foreach (var resultDrawing in render.ResultDrawings)
            {
                this.HSmartWindowControlWpf.HalconWindow.SetColor(ColorConverter.ToString(resultDrawing.color));

                using var drawingRegion = resultDrawing.drawingObject.AffineTransformRegion(alignContext.TransformMatrixInvert);

                this.HSmartWindowControlWpf.HalconWindow.DispObj(drawingRegion);
            }

            displayText(isFound ? offset.ToString() : "NOT FOUND", 5, 5, color: (isFound ? "green" : "red"));
        }

        public override void SetCameraType(ECamera type)
        {
            this.CameraType = type;
        }

        private void displayText(string text, int row, int col, string color = "green", ECoordinateSystem coordinate = ECoordinateSystem.Image, string font = "default-20", bool box = false, string boxColor = "green")
        {
            this.HSmartWindowControlWpf.HalconWindow.SetFont(font);
            this.HSmartWindowControlWpf.HalconWindow.SetColor(color);
            this.HSmartWindowControlWpf.HalconWindow.DispText(text, coordinate.ToString().ToLower(), row, col, color, new HTuple("box", "box_color"), new HTuple(box.ToString().ToLower(), boxColor));
        }
    }
}