using CommunityToolkit.Mvvm.Messaging;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.DomainLayer.Services.Teaching;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using System.Threading.Tasks;


namespace GVisionWpf.DomainLayer.Services.Running
{
    public partial class PrsInspectionHandler
    {
        private static EInspection PrsInspectionType => DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType;

        private static EInspectionMode InspectionMode => GlobalSetting.Instance.Inspection.Mode;

        private EmapService emapService;

        private MoldTeachingInspectionService moldInspection;
        private LgaTeachingInspectionService lgaInspection;
        private QfnTeachingInspectionService qfnInspection;
        private BgaTeachingInspectionService bgaInspection;

        private DisposeBag shotDisposeBag = new DisposeBag();

        public PrsInspectionHandler()
        {
            this.moldInspection = new MoldTeachingInspectionService();
            this.lgaInspection = new LgaTeachingInspectionService();
            this.qfnInspection = new QfnTeachingInspectionService();
            this.bgaInspection = new BgaTeachingInspectionService();

            this.emapService = EmapService.Instance;

            GVisionMessenger.Instance.Register(this);
        }

        public async Task<InspectionResult> Inspect(HObject image, int pickerNo, EachPrsBody prsBody)
        {
            bool pickerInUse = prsBody.HasDevice == 1;

            RenderableInspectionResult result;

            image.DisposeBy(shotDisposeBag);

            if (!pickerInUse)
            {
                result = new RenderableInspectionResult(new SkipInspectionResult { Image = image }, new InspectionRenderData());
                result.RenderData.AddText(new FixedText("SKIP", 1, EColor.Green, 30));
            }
            else
            {
                result = PrsInspectionType switch {
                    EInspection.Mark => (await this.moldInspection.InspectAsync(
                        [image],
                        MoldRepository.Instance.GetRecipe(),
                        ECamera.PRS,
                        GlobalSetting.Instance.Inspection.MoldItems
                        ))
                        .First(),

                    EInspection.Bga => (await this.bgaInspection.InspectAsync(
                        [image],
                        BgaRepository.Instance.GetRecipe(),
                        ECamera.PRS,
                        GlobalSetting.Instance.Inspection.BgaItems
                        ))
                        .First(),

                    EInspection.Lga => (await this.lgaInspection.InspectAsync(
                        [image],
                        LgaRepository.Instance.GetRecipe(),
                        ECamera.PRS,
                        GlobalSetting.Instance.Inspection.LgaItems
                        ))
                        .First(),

                    EInspection.Qfn => (await this.qfnInspection.InspectAsync(
                        [image],
                        QfnRepository.Instance.GetRecipe(),
                        ECamera.PRS,
                        GlobalSetting.Instance.Inspection.QfnItems
                        ))
                        .First(),

                    _ => throw new InvalidOperationException($"Unsupported inspection type: {PrsInspectionType}")
                };

                result.InspectionResult.XPosition = (int)prsBody.XPickPosition;

                result.InspectionResult.YPosition = (int)prsBody.YPickPosition;

                #region Handle AllPass
                if (InspectionMode == EInspectionMode.AllPass)
                {
                    result.InspectionResult.cachedErrorType = EResultType.Good;

                    result.RenderData.ClearTexts();
                    result.RenderData.AddText(new FixedText("ALL PASS", 1, EColor.Green, 40));
                }
                #endregion

                #region Handle XOut

                switch (GlobalSetting.Instance.SystemType)
                {
                    case ESystemType.HanaMicron:
                        switch (prsBody.StripBarcode)
                        {
                            // XOut 1
                            case (int)EEmapDataType.XOUT_1:
                                result.InspectionResult.XOut = new Result<bool>(EResultType.XOut, true);
                                result.InspectionResult.cachedErrorType = EResultType.XOut;
                                result.RenderData.ClearTexts();
                                result.RenderData.AddText(new FixedText("X-OUT", 1, EColor.Red, 40));
                                break;

                            // XOut 2
                            case (int)EEmapDataType.XOUT_2:
                                result.InspectionResult.XOut = new Result<bool>(EResultType.XOut2, true);
                                result.InspectionResult.cachedErrorType = EResultType.XOut2;
                                result.RenderData.ClearTexts();
                                result.RenderData.AddText(new FixedText("X-OUT2", 1, EColor.Red, 40));
                                break;

                            default:
                                break;
                        }
                        break;

                    default:
                        var emapResults = await this.emapService.IsXOut((int)prsBody.GridTableNumber, result.InspectionResult.XPosition, result.InspectionResult.YPosition);

                        if (!emapResults.IsNullOrEmpty())
                        {
                            var emapData = (EEmapDataType)emapResults.First().Data;

                            switch (emapData)
                            {
                                case EEmapDataType.XOUT_1:
                                    result.InspectionResult.XOut = new Result<bool>(EResultType.XOut, true);
                                    result.InspectionResult.cachedErrorType = EResultType.XOut;
                                    result.RenderData.ClearTexts();
                                    result.RenderData.AddText(new FixedText("X-OUT", 1, EColor.Red, 40));
                                    break;

                                case EEmapDataType.XOUT_2:
                                    result.InspectionResult.XOut = new Result<bool>(EResultType.XOut2, true);
                                    result.InspectionResult.cachedErrorType = EResultType.XOut2;
                                    result.RenderData.ClearTexts();
                                    result.RenderData.AddText(new FixedText("X-OUT2", 1, EColor.Red, 40));
                                    break;

                                default:
                                    break;
                            }
                        }

                        break;
                }

                #endregion


                GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.AddInspectionResult, result, pickerNo, prsBody);
            }

            RenderableInspectionResult resultsForPickerDeviceView = new RenderableInspectionResult((InspectionResult)result.InspectionResult, result.RenderData);

            GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.UpdatePickerResult, resultsForPickerDeviceView, pickerNo, prsBody);

            result.InspectionResult.PackageOffset.Value += GlobalSetting.Instance.OffsetCompensation[(EMultiPicker)prsBody.X1Orx2][pickerNo];

            return result.InspectionResult;
        }
    }

    public partial class PrsInspectionHandler : IRecipient<PrsInspectionUIUpdateMessage>
    {
        public void Receive(PrsInspectionUIUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case EPrsInspectionUIUpdateType.ClearAllResults:
                    shotDisposeBag.Dispose();
                    break;

                default:
                    break;
            }
        }
    }
}
