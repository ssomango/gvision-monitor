using GVisionWpf.Cameras;
using GVisionWpf.Cameras.CamearaQueues;
using GVisionWpf.DomainLayer.Services.Running;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Dtos.Response;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.Visions;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.ViewModels;
using log4net;
using MahApps.Metro.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace GVisionWpf.PresentationLayer.Controllers
{
    public class PrsController : BaseController
    {
        private static readonly Lazy<PrsController> lazy = new Lazy<PrsController>(() => new PrsController());
        public static PrsController Instance => lazy.Value;

        private readonly HistoryService historyService;
        private readonly LotService lotService;
        private readonly PrsInspectionHandler inspectionHandler;
        private readonly Camera camera = CameraManager.Instance.Cameras[ECamera.PRS];

        protected TaskQueue prsTaskQueue;


        private readonly EInspection inspectionType;

        private static readonly ILog log = LogManager.GetLogger("PRS");

        private const string TASK_NAME = "PrsInspection";
        private const uint SW_PACKET = 0u;
        private const uint HW_PACKET = 1u;
        private const uint TEST_PACKET = 999;


        protected PrsController()
        {
            this.historyService = HistoryService.Instance;
            this.lotService = LotService.Instance;
            this.inspectionHandler = new PrsInspectionHandler();

            this.inspectionType = DeviceRecipeRepository.Instance.GetRecipe().PrsPackageType;
            this.prsTaskQueue = new TaskQueue(1);
        }

        public async void PrsInspection(PrsRequest prsRequest)
        {
            log.Info($"[Request] PRS ({prsRequest})");


            // Clean Prs image Que
            // IOtrigger can't this
            if (PrsImageQueue.Instance.Count() != 0)
            {
                log.Info("remain iamges in the Que, clean up");
                GVisionMessenger.Instance.UI.SendSystemInfoMessage("remain iamges in the Que, clean up");
                PrsImageQueue.Instance.Clear();
            }

            await sendClearAllResultsMessageIfFirstDeviceAsync(prsRequest);

            if (camera.cameraTriggerMode == ECameraTriggerMode.IOTrigger)
                GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.ClearPickerResult, null, null, prsRequest.PrsBodies.First());

            acquireInspectionImage(prsRequest, completion: ((EachPrsBody prsBody, int pickerNo, HObject image) context) =>
            {
                trySendClearPickerResultMessage(context.pickerNo, context.prsBody);

                bool pickerInUse = context.prsBody.HasDevice == 1;

                this.prsTaskQueue.EnqueueTask(async (cancellationToken) =>
                {
                    InspectionResult result = await this.inspectionHandler.Inspect(context.image, context.pickerNo, context.prsBody);
                    
                    PrsResponse response = new PrsResponse
                    {
                        CommonBody = prsRequest.CommonBody!,
                        InspectionResult = result.EvaluateResults() ? 1u : 0u,
                        ErrorType = InspectionResultConverter.ErrorType2ErrorCode(result.ErrorType(), this.inspectionType),
                        PrsBody = context.prsBody,
                    };
                    // SECGEME

                    switch (GlobalSetting.Instance.SystemType)
                    {
                        case (ESystemType.HanaMicron):
                            if (result.ErrorType() != EResultType.NoDevice)
                            {
                                response.PrsBody.StripBarcode = (uint)(result.SawOffset.Value?.X ?? 0);
                                response.PrsBody.Sequence = (uint)(result.SawOffset.Value?.Y ?? 0);
                                response.PrsBody.XPickPosition = (uint)(result.PackageSize.Value.Width);
                                response.PrsBody.YPickPosition = (uint)(result.PackageSize.Value.Height);
                            }
                            break;

                        default:
                            response.PrsBody.StripBarcode = 0;
                            response.PrsBody.Sequence = 0;
                            response.PrsBody.XPickPosition = 0;
                            response.PrsBody.YPickPosition = 0;
                            break;
                    }

                    response.CommonBody.DataLength = 72;
                    response.CommonBody.CommonHeader = 1;

                    SetOffset(response, result.PackageOffset.Value, TASK_NAME);

                    Respond(response);
                    
                    if (pickerInUse)
                    {
                        SaveImage(context.image, this.inspectionType, result.ErrorType(), result.XPosition, result.YPosition, null, out string? imagePath);

                        SaveDeepLearningImage(context.image, this.inspectionType, result.ErrorType(), result.XPosition, result.YPosition, null);

                        await this.lotService.CreateInspectionResult(this.inspectionType, result, result.XPosition, result.YPosition, GlobalSetting.Instance.DeviceInfo.LotId);

                        if (!result.EvaluateResults())
                        {
                            await this.historyService.CreateHistory(GlobalSetting.Instance.DeviceInfo.LotId, GlobalSetting.Instance.DeviceInfo.RecipeName, result.ToString(), ELog.LOTLogs, ECamera.PRS, this.inspectionType, imagePath);
                        }
                    }
                    
                    log.Info($"[Response] PRS ({response})");
                });
            });

            if (PrsImageQueue.Instance.Count() != 0)
            {
                log.Info("A prs packit is done, image Que clear");
                PrsImageQueue.Instance.Clear();
            }
        }

        private void acquireInspectionImage(PrsRequest request, Action<(EachPrsBody PrsBody, int PickerNo, HObject image)> completion)
        {
            for (int i = 0; i < request.PrsBodies.Count; i++)
            {
                EachPrsBody prsBody = request.PrsBodies[i];

                if (camera.cameraTriggerMode == ECameraTriggerMode.IOTrigger)
                {
                    if (prsBody.HasDevice == 0)
                        continue;
                }   

                int pickerNo = request.PrsBodies.Count - i - 1;

                switch (request.TriggerType)
                {  
                    case HW_PACKET:
                        if (this.camera.cameraTriggerMode == ECameraTriggerMode.HardwareTrigger ||
                            this.camera.cameraTriggerMode == ECameraTriggerMode.IOTrigger)
                        {
                            try
                            {
                                HObject hwImage = CameraManager.Instance.PrsQueue.Dequeue(4500);
                                completion.Invoke((PrsBody: prsBody, PickerNo: pickerNo, image: hwImage));
                            }
                            catch
                            {
                                GVisionMessenger.Instance.UI.SendSystemInfoMessage("prs time out");
                                return;

                            }
                        }
                        else
                        {
                            log.Info($"[check up camera json] PRS cameraTriggerMode to IO or HW");
                            return;
                        }
                        break;

                    case SW_PACKET:
                        if (this.camera.cameraTriggerMode == ECameraTriggerMode.SoftwareTrigger)
                        {
                            if (prsBody.ZAxisNum == 0xff)
                                continue;

                            camera.TriggerShot();
                            HObject swImage = CameraManager.Instance.RetrieveTriggeredImage(ECamera.PRS);
                            sendCapDonePacket(request.CommonBody!, prsBody);
                            completion.Invoke((PrsBody: prsBody, PickerNo: pickerNo, image: swImage));
                        }
                        break;

                    case TEST_PACKET:
                        HObject image = CameraManager.Instance.RetrieveImage(ECamera.PRS);
                        completion.Invoke((PrsBody: prsBody, pickerNo: pickerNo, image: image));
                        break;
                }
            }
        }

        private void sendCapDonePacket(CommonBody commonBody, EachPrsBody prsBody)
        {
            CapDoneResponse response = new CapDoneResponse
            {
                CommonBody = commonBody,
                InspectionResult = 2u,
                CaptureDone = 0u,
                StripBarcode = 0,
                Sequence = 0,
                GridTableNumber = prsBody.GridTableNumber,
                XPickPosition = prsBody.XPickPosition,
                YPickPosition = prsBody.YPickPosition
            };

            response.CommonBody.DataLength = 60;
            response.CommonBody.CommonHeader = 1;
            Respond(response);
        }

        private async Task sendClearAllResultsMessageIfFirstDeviceAsync(PrsRequest request)
        {
            await Parallel.ForEachAsync(request.PrsBodies, async (prsBody, cancellationToken) =>
            {
                var pickerNo = request.PrsBodies.Count - request.PrsBodies.IndexOf(prsBody) - 1;

                if (prsBody.HasDevice == 1 && prsBody.YPickPosition == 0 && prsBody.XPickPosition == 0 && prsBody.ZAxisNum != 0xff)
                {
                    GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.ClearAllResults);
                }
            });
        }


        private void trySendClearPickerResultMessage(int pickerNo, EachPrsBody prsBody)
        {
            if (pickerNo == 7 && camera.cameraTriggerMode != ECameraTriggerMode.IOTrigger)
            {
                GVisionMessenger.Instance.UI.SendPrsUIUpdate(EPrsInspectionUIUpdateType.ClearPickerResult, null, null, prsBody);
            }
        }
    }
}
