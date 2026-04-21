using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.DomainLayer.Data.Inspection.Item.DataCode;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Extensions;
using GVisionWpf.DomainLayer.Data.Inspection.Result.DataCode;
using GVisionWpf.DomainLayer.Data.Alignment;

namespace GVisionWpf.DomainLayer.Services.Teaching.DataCode
{
    public sealed partial class DataCodeTeachingService<TTeaching, TResult, TItem> : IDataCodeTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {

        private IDataCodeItemProvider<TItem>? dataCodeProvider = DataCodeItemProviderFactory.GetProvider<TItem>();

        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        private HObject preprocessInspectionImage(AlignContext alignContext, TTeaching teaching)
        {
            HObject inspectionImage;

            if (teaching is ISinglePackageTeachingModel<TTeaching> singlePackageTeaching)
            {
                HOperatorSet.ErosionCircle(alignContext.PackageRegion, out HObject erosionPackageRegion, 2);
                VisionOperation.ReduceDomain(alignContext.AlignedImage, erosionPackageRegion, out inspectionImage);
                erosionPackageRegion.Dispose();
            }
            else if (teaching is IGridPackageTeachingModel<TTeaching> gridPackageTeaching)
            {
                inspectionImage = alignContext.AlignedImage;
            }
            else
            {
                throw new Exception($"Unsupported teaching model type: {teaching.GetType()}");
            }

            // Omit DontCare
            if (teaching is IDontCareTeachingModel<TTeaching> dontCareTeaching
                && !dontCareTeaching.DontCareRois.IsNullOrEmpty())
            {
                inspectionImage = inspectionImage.OmitRegionFromTarget(dontCareTeaching.DontCareRois.ToList(), 2);
            }

            return inspectionImage;
        }

        public IDataCodeInspectionResultModel<TResult> InspectDataCodes(AlignContext alignContext, IDataCodeTeachingModel<TTeaching> teaching, bool enforceAllChecks, HashSet<TItem> inspectionItems, out InspectionRenderData renderData)
        {

            IDataCodeInspectionResultModel<TResult> result = (IDataCodeInspectionResultModel<TResult>)new TResult();

            renderData = new InspectionRenderData();


            if ((dataCodeProvider is not null && inspectionItems.Contains(dataCodeProvider.DataCode)) || enforceAllChecks)
            {
                if (teaching.CodeRoi is null) return result;

                var inspectionImage = preprocessInspectionImage(alignContext, (TTeaching)teaching);

                HOperatorSet.CreateDataCode2dModel("Data Matrix ECC 200", new HTuple(), new HTuple(), out HTuple dataCodeHandler);
                HOperatorSet.SetDataCode2dParam(dataCodeHandler, "default_parameters", "maximum_recognition");
                HOperatorSet.SetDataCode2dParam(dataCodeHandler, "polarity", "light_on_dark");
                HOperatorSet.SetDataCode2dParam(dataCodeHandler, "timeout", 1000);

                using (dataCodeHandler)
                {
                    var dataCode = MapEngine.InspectDataCode(inspectionImage, teaching.CodeRoi, dataCodeHandler, out HObject symbolXLDs);

                    HObject codeRegion = teaching.CodeRoi
                        .Roi2Region()
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);

                    HObject symbolRegion = symbolXLDs
                        .AffineTransformRegion(alignContext.TransformMatrixInvert)
                        .DisposeBy(DisposeBag);


                    if (dataCode.Type == EResultType.Good)
                    {
                        renderData.ResultDrawings.Add((drawingObject: symbolRegion, EColor.Green));
                    }
                    else
                    {
                        renderData.ResultDrawings.Add((drawingObject: codeRegion, EResultType.DataCode.GetResultColor((InspectionTeaching)teaching)));
                    }

                    dataCodeHandler.ClearHandle();

                    result.DataCode = dataCode;

                    return result;
                }
            }

            return result;
        }
    }
}
