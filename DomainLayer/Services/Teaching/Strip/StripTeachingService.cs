using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Strip;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Visions.Engines;
using GVisionWpf.Visions;
using System.Diagnostics;
using GVisionWpf.GlobalStates;
using GVisionWpf.DomainLayer.Extensions;

namespace GVisionWpf.DomainLayer.Services.Teaching.Strip
{
    public class StripTeachingService<TTeaching, TResult, TItem> : IStripTeachingService<TTeaching, TResult, TItem>
        where TTeaching : InspectionTeaching
        where TResult : InspectionResult, new()
        where TItem : IInspectionItem
    {
        public DisposeBag DisposeBag { get; private set; } = new DisposeBag();

        public void Dispose() => DisposeBag.Dispose();

        public IStripInspectionResultModel<TResult> InspectStripDataCode(HObject image, IStripTeachingModel<TTeaching> teaching, out InspectionRenderData renderData)
        {
            renderData = new InspectionRenderData();

            IStripInspectionResultModel<TResult>? result = (IStripInspectionResultModel<TResult>)new TResult();

            HOperatorSet.CreateDataCode2dModel("Data Matrix ECC 200", new HTuple(), new HTuple(), out HTuple dataCodeHandler);
            HOperatorSet.SetDataCode2dParam(dataCodeHandler, "default_parameters", "maximum_recognition");
            HOperatorSet.SetDataCode2dParam(dataCodeHandler, "polarity", "light_on_dark");
            HOperatorSet.SetDataCode2dParam(dataCodeHandler, "timeout", 100);

            using (dataCodeHandler)
            {
                foreach (var stripRoi in teaching.StripRois)
                {
                    var stripResult = MapEngine.InspectDataCode(image, stripRoi, dataCodeHandler, out HObject symbolXLDs);

                    using (symbolXLDs)
                    {
                        if (stripResult.Type == EResultType.Good)
                        {
                            result.StripDataCode = stripResult;
                            VisionOperation.Roi2Region(stripRoi, out HObject dataCodeRegion);
                            EColor color = result.StripDataCode.Type == EResultType.Good ? EColor.Green : EColor.Red;

                            renderData.ResultDrawings.Add((drawingObject: dataCodeRegion, color: color));


                            HOperatorSet.GenRegionContourXld(symbolXLDs, out HObject region, "filled");

                            HOperatorSet.AreaCenter(region, out _, out HTuple row, out HTuple column);

                            VisionOperation.GetCenterPointOfRegion(image, out Point imageCenterPoint);

                            int centerXPos = (int)(column.D - imageCenterPoint.Col);
                            int centerYPos = (int)(row.D - imageCenterPoint.Row);

                            if (GlobalSetting.Instance.SystemType == ESystemType.HanaMicron)
                            {
                                // 하나마이크론 장비는 mm 단위를 기준으로, 1000을 곱해서 달라고 함.
                                // 제어에서 다시 수치를 가공해서 사용한다고 함.
                                centerXPos *= 10;
                                centerYPos *= 10;
                            }

                            renderData.ResultDrawings.Add((region, EColor.Green));
                            region.DisposeBy(DisposeBag);

                            result.XOffset = new Result<int>(EResultType.Good, centerXPos);
                            result.YOffset = new Result<int>(EResultType.Good, centerYPos);

                            renderData.AddText(new FixedText($"DataCode : {stripResult.Value}\nX Offset : {centerXPos}, Y Offset : {centerYPos}", 1, EColor.Green));

                            return result;
                        }
                    }
                }

                return result;
            }
        }
    }
}
