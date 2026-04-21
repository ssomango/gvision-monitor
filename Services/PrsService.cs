using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Services
{
    public abstract class PrsService : BaseService
    {
        protected PrsService() { }

        public abstract List<InspectionResult> Inspect(HObject image, EInspectionMode inspectionMode);
        public abstract uint GetErrorCodeForHandler(EResultType resultType);
    }
}