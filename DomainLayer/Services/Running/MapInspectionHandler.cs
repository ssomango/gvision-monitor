using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.DomainLayer.Services.Running
{
    internal class MapInspectionHandler
    {
        private static Device CurrentDevice => DeviceRecipeRepository.Instance.GetRecipe();
        
        private DeviceCoordinateService deviceCoordinateServce;

        private EInspection MapInspectionType => CurrentDevice.MapPackageType;

        private EInspectionMode InspectionMode => GlobalSetting.Instance.Inspection.Mode;

        public EmapService emapService;

        private GridMoldTeachingInspectionService moldInspection;
        private GridLgaTeachingInspectionService lgaInspection;
        private GridQfnTeachingInspectionService qfnInspection;
        private GridBgaTeachingInspectionService bgaInspection;

        public MapInspectionHandler()
        {
            this.deviceCoordinateServce = new DeviceCoordinateService(CurrentDevice);

            this.moldInspection = new GridMoldTeachingInspectionService();
            this.lgaInspection = new GridLgaTeachingInspectionService();
            this.qfnInspection = new GridQfnTeachingInspectionService();
            this.bgaInspection = new GridBgaTeachingInspectionService();

            this.emapService = EmapService.Instance;
        }

        public async Task<List<InspectionResult>> Inspect(List<HObject> images, MapBody mapBody)
        {
            List<RenderableInspectionResult> results = MapInspectionType switch
            {
                EInspection.Mark => (await this.moldInspection.InspectAsync(
                    images,
                    GridMoldRepository.Instance.GetRecipe(),
                    ECamera.Mapping,
                    GlobalSetting.Instance.Inspection.MoldItems
                    ))
                    .ToList(),

                EInspection.Qfn => (await this.qfnInspection.InspectAsync(
                    images,
                    GridQfnRepository.Instance.GetRecipe(),
                    ECamera.Mapping,
                    GlobalSetting.Instance.Inspection.QfnItems
                    ))
                    .ToList(),

                EInspection.Bga => (await this.bgaInspection.InspectAsync(
                    images,
                    GridBgaRepository.Instance.GetRecipe(),
                    ECamera.Mapping,
                    GlobalSetting.Instance.Inspection.BgaItems
                    ))
                    .ToList(),

                EInspection.Lga => (await this.lgaInspection.InspectAsync(
                    images,
                    GridLgaRepository.Instance.GetRecipe(),
                    ECamera.Mapping,
                    GlobalSetting.Instance.Inspection.LgaItems
                   ))
                   .ToList(),

                _ => throw new InvalidOperationException($"Unsupported inspection type: {MapInspectionType}")
            };

            var xVTPosition = (int)mapBody.XPosition;
            var yVTPosition = (int)mapBody.YPosition;

            var lastCoordinate = this.deviceCoordinateServce.GetLastCoordinateOfBlock(xVTPosition, yVTPosition);

            var fovSize = DeviceRecipeRepository.Instance.GetRecipe().FovSize;
            var fov = fovSize.Col * fovSize.Row;

            for (int i = 0; i < results.Count; i++)
            {
                this.deviceCoordinateServce.CalculateFovPos(
                    xVTPosition: xVTPosition,
                    yVTPosition: yVTPosition,
                    XPositionInFOV: i % (CurrentDevice.FovSize).Col,
                    YPositionInFOV: i / (CurrentDevice.FovSize).Col,
                    fovSize: CurrentDevice.FovSize,
                    out int xPositionForGrid,
                    out int yPositionForGrid
                    );

                results[i].InspectionResult.XPosition = xPositionForGrid;
                results[i].InspectionResult.YPosition = yPositionForGrid;

                #region Handle AllSkip
                if (InspectionMode == EInspectionMode.AllPass)
                {
                    results[i].InspectionResult.cachedErrorType = EResultType.Good;

                    results[i].RenderData.ClearTexts();
                    results[i].RenderData.AddText(new FixedText("ALL PASS", 1, EColor.Green, 40));
                }
                #endregion


                #region Handle Xout

                var emapResults = await this.emapService.IsXOut((int)mapBody.GridTableNum, xPositionForGrid, yPositionForGrid);

                if (!emapResults.IsNullOrEmpty())
                {
                    EEmapDataType emapData = (EEmapDataType)emapResults.First().Data;

                    switch (emapData)
                    {
                        case EEmapDataType.XOUT_1:
                            results[i].InspectionResult.XOut = new Result<bool>(EResultType.XOut, true);
                            results[i].InspectionResult.cachedErrorType = EResultType.XOut;
                            results[i].RenderData.ClearTexts();
                            results[i].RenderData.AddText(new FixedText("X-OUT", 1, EColor.Red, 40));
                            break;
                        case EEmapDataType.XOUT_2:
                            results[i].InspectionResult.XOut = new Result<bool>(EResultType.XOut2, true);
                            results[i].InspectionResult.cachedErrorType = EResultType.XOut2;
                            results[i].RenderData.ClearTexts();
                            results[i].RenderData.AddText(new FixedText("X-OUT2", 1, EColor.Red, 40));
                            break;

                        default:
                            break;
                    }
                }

                #endregion
            };

            results = results
                .FindAll(r => r.InspectionResult.XPosition <= lastCoordinate.Col && r.InspectionResult.YPosition <= lastCoordinate.Row)
                .ToList();

            int nGoodDevices = results.Count(result => result.InspectionResult.EvaluateResults());
            int nBadDevices = results.Count(result => !result.InspectionResult.EvaluateResults());

            FixedText totalText = new FixedText($"TOTAL: {nGoodDevices + nBadDevices}, GOOD: {nGoodDevices}, BAD: {nBadDevices}", 1, nBadDevices > 0 ? EColor.Red : EColor.Green);

            results.ForEach(r => r.RenderData.ClearTexts());
            results.ForEach(r => r.RenderData.AddText(totalText));

            GVisionMessenger.Instance.UI.SendMappingUIUpdate(EMoldInspectionUIUpdateType.AddInspectionResult, results, mapBody);

            return results.Select(r => r.InspectionResult).ToList();
        }
    }
}
