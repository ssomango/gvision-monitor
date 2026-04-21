using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using System.Threading.Tasks;

namespace GVisionWpf.DomainLayer.Services.Running
{
    public class StripInspectionHandler
    {

        private StripTeachingInspectionService stripInspection;

        private const int TOP_BARCODE = 40;
        private const int BOTTOM_BARCODE = 41;

        public StripInspectionHandler()
        {
            stripInspection = new StripTeachingInspectionService();
        }

        public async Task<InspectionResult> Inspect(HObject image, ECamera camera, ERequestInspectionType inspectionType, StripBarcodeRequest stripRequest)
        {
            RenderableInspectionResult result;

            result = (await stripInspection.InspectAsync(
                images: [image], 
                teaching: StripRepository.Instance.GetRecipe(),
                camera: camera, 
                inspectionItems: []
                ))
                .First();

            GVisionMessenger.Instance.UI.SendStripUIUpdate(EStripInspectionUIUpdateType.AddInspectionResult, inspectionType, result);

            return result.InspectionResult;
        }
    }
}
