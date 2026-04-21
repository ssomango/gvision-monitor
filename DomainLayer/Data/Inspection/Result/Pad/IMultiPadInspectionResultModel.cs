using GVisionWpf.DomainLayer.Data.Inspection.Result;
using GVisionWpf.DomainLayer.Data.Inspection.Result.MultiPad;
using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.Interfaces.MultiPad
{
    public partial interface IMultiPadInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> MultiPadCount { get; set; }

        public Result<StatisticalList<Pose>> MultiPadOffset { get; set; }

        public Result<StatisticalList<Ratio>> MultiPadArea { get; set; }

        IMultiPadInspectionResultModel<T> MergeTo(IMultiPadInspectionResultModel<T> model)
        {
            model.MultiPadPitch = this.MultiPadPitch;
            model.MultiPadPerimeter = this.MultiPadPerimeter;
            model.MultiPadCount = this.MultiPadCount;
            model.MultiPadOffset = this.MultiPadOffset;
            model.MultiPadContamination = this.MultiPadContamination;
            model.MultiPadSize = this.MultiPadSize;
            model.MultiPadArea = this.MultiPadArea;
            return model;
        }
    }

    public partial interface IMultiPadInspectionResultModel<T> : IMultiPadPerimeterInspectionResultModel<T> { }

    public partial interface IMultiPadInspectionResultModel<T> : IMultiPadContaminationResultModel<T> { }

    public partial interface IMultiPadInspectionResultModel<T> : IMultiPadSizesResultModel<T> { }

    public partial interface IMultiPadInspectionResultModel<T> : IMultiPadPtichesResultModel<T> { }
}
