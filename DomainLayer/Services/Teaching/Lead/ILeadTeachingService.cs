using GVisionWpf.DomainLayer.Data.Alignment;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Interfaces.Inspect.Lead;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Services.Teaching.Lead
{
    public interface ILeadTeachingService<TLeadTeaching, TLeadResult, TLeadItem> : IDisposable
        where TLeadTeaching : InspectionTeaching
        where TLeadResult : InspectionResult
        where TLeadItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; }

        public ILeadTeachingModel<TLeadTeaching> TeachLeads(HObject teachingImage, HObject packageRegion, ECamera camera, ILeadTeachingModel<TLeadTeaching> teaching, bool enforceAllChecks, HashSet<TLeadItem> inspectionItems, out InspectionRenderData renderData);

        public ILeadInspectionResultModel<TLeadResult> InspectLeads(AlignContext alignContext, ILeadTeachingModel<TLeadTeaching> teaching, bool enforceAllChecks, HashSet<TLeadItem> inspectionItems, out InspectionRenderData renderData);
    }
}
