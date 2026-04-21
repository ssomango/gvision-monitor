using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GVisionWpf.Cameras;
using GVisionWpf.Cameras.CamearaQueues;
using GVisionWpf.DomainLayer.Services.Running;
using GVisionWpf.DSMMI.Inspection;
using GVisionWpf.Events.Message.Inspection;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Dtos.Response;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.Types;
using GVisionWpf.UIs.UiUpdaters;
using log4net;


namespace GVisionWpf.PresentationLayer.Controllers
{
    public class MapController : BaseController
    {
        private static readonly Lazy<MapController> lazy = new Lazy<MapController>(() => new MapController());
        public static MapController Instance => lazy.Value;

        private readonly HistoryService historyService;
        private readonly LotService lotService;
        private readonly IlluminationService illuminationService;
        private readonly DeviceCoordinateService deviceCoordinateServce;

        private readonly MapInspectionHandler inspectionHandler;
        private readonly Camera camera = CameraManager.Instance.Cameras[ECamera.Mapping];

        private readonly EInspection inspectionType;

        protected TaskQueue MapTaskQueue = new TaskQueue(1);
        private CancellableTaskQueue ControllerTaskQueue = new CancellableTaskQueue(1);

        private Guid latestControllerTaskId;

        private static readonly ILog log = LogManager.GetLogger("MAP");

        private const string TASK_NAME = "MappingInspection";
        private const uint SW_PACKET = 0u;
        private const uint HW_PACKET = 1u;
        private const uint TEST_PACKET = 999;


        private MapController()
        {
            this.historyService = HistoryService.Instance;
            this.lotService = LotService.Instance;

            this.illuminationService = IlluminationService.Instance;
            this.deviceCoordinateServce = new DeviceCoordinateService(DeviceRecipeRepository.Instance.GetRecipe());

            this.inspectionHandler = new MapInspectionHandler();
            this.inspectionType = DeviceRecipeRepository.Instance.GetRecipe().MapPackageType;
        }

        public async void MapInspection(MapRequest mapRequest)
        {
            log.Info($"[Request] Mapping ({mapRequest})");

            if (MappingImageQueue.Instance.Count() != 0)
            {
                log.Info("remain iamges in the map Que, clean up");
                GVisionMessenger.Instance.UI.SendSystemInfoMessage("remain iamges in the map Que, clean up");
                MappingImageQueue.Instance.Clear();
            }

            var prevoiusTaskId = latestControllerTaskId;

            cancelPreviousTaskIfNeeded(mapRequest, prevoiusTaskId);

            latestControllerTaskId = ControllerTaskQueue.EnqueueCancelableTask(async (cancellationToken) =>
            {
                resetOnFirstPosition(mapRequest);

                await acquireInspectionImage(mapRequest, cancellationToken, completion: async ((MapBody mapBody, List<HObject> shots) context) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await MapTaskQueue.EnqueueTask(async (_) =>
                    {
                        try
                        {
                            List<InspectionResult> results = await this.inspectionHandler.Inspect(context.shots, context.mapBody);

                            saveMultiShotImage(results, (int)context.mapBody.XPosition, (int)context.mapBody.YPosition, out List<string> imagePaths);

                            foreach (InspectionResult result in results)
                            {
                                MapResponse response = new MapResponse
                                {
                                    CommonBody = mapRequest.CommonBody!,
                                    InspectionResult = result.EvaluateResults() ? 1u : 0u,
                                    ErrorType = InspectionResultConverter.ErrorType2ErrorCode(result.ErrorType(), this.inspectionType),


                                    MapBody = context.mapBody,
                                };

                                // SECGEME

                                switch (GlobalSetting.Instance.SystemType)
                                {
                                    case ESystemType.HanaMicron:
                                        if (result.ErrorType() != EResultType.NoDevice)
                                        {
                                            response.XOffset = (int)(result.PackageSize.Value.Width);
                                            response.YOffset = (int)(result.PackageSize.Value.Height);
                                        }
                                        break;

                                    default:
                                        SetOffset(response, new Pose(0, 0, 0), TASK_NAME);
                                        break;
                                }

                                response.CommonBody!.DataLength = 60;
                                response.CommonBody.CommonHeader = 0x01;

                                response.MapBody.XPosition = (uint)result.XPosition;
                                response.MapBody.YPosition = (uint)result.YPosition;

                                Respond(response);

                                log.Info($"[Response] Mapping ({response})");

                                if (!result.EvaluateResults())
                                {
                                    await this.historyService.CreateHistory(GlobalSetting.Instance.DeviceInfo.LotId,
                                                                            GlobalSetting.Instance.DeviceInfo.RecipeName,
                                                                            result.ToString(),
                                                                            ELog.LOTLogs,
                                                                            ECamera.Mapping,
                                                                            this.inspectionType,
                                                                            imagePaths.Any() ? imagePaths.First() : null);
                                }

                                await this.lotService.CreateInspectionResult(this.inspectionType, result, result.XPosition, result.YPosition, GlobalSetting.Instance.DeviceInfo.LotId);
                            }
                        }
                        catch(Exception ex)
                        {
                            GlobalErrorHandler.HandleException(ex);
                        }
                    });
                });
            });
        }

        private void resetOnFirstPosition(MapRequest request)
        {
            if (request.MapBody is null) return;

            int xPosition = (int)request.MapBody.XPosition;
            int yPosition = (int)request.MapBody.YPosition;

            if (xPosition == 0 && yPosition == 0)
            {
                GVisionMessenger.Instance.UI.SendMappingUIUpdate(EMoldInspectionUIUpdateType.ClearAllResults);
            }
        }

        private void cancelPreviousTaskIfNeeded(MapRequest request, Guid cancelTaskId)
        {
            if (camera.cameraTriggerMode == ECameraTriggerMode.IOTrigger)
            {
                if (ControllerTaskQueue.RunningTaskCount > 0)
                {
                    ControllerTaskQueue.CancelTask(cancelTaskId);
                }
            }
        }

        private async Task acquireInspectionImage(MapRequest request, CancellationToken cancellationToken, Func<(MapBody mapBody, List<HObject> shots), Task> completion)
        {
            switch (request.TriggerType)
            {
                case HW_PACKET:
                    if (camera.cameraTriggerMode == ECameraTriggerMode.IOTrigger)
                    {
                        deviceCoordinateServce.CalculateNTotalFov(out int nTotalYFov, out int nTotalXFov);
                        var totalShotCount = nTotalXFov * nTotalYFov;

                        var fovCol = deviceCoordinateServce.FOVSize.Col;
                        var fovRow = deviceCoordinateServce.FOVSize.Row;

                        var maxCol = deviceCoordinateServce.VisionTableSize.Col;
                        var maxRow = deviceCoordinateServce.VisionTableSize.Row;

                        int xPosition = 0;
                        int yPosition = 0;

                        for (int i = 0; i < totalShotCount; i++)
                        {
                            try
                            {
                                var shot = CameraManager.Instance.MappingQueue.Dequeue(cancellationToken);

                                var mapBody = new MapBody
                                {
                                    StripBarcode = request.MapBody.StripBarcode,
                                    Sequence = request.MapBody.Sequence,
                                    GridTableNum = request.MapBody.GridTableNum,
                                    XPosition = (uint)xPosition,
                                    YPosition = (uint)yPosition
                                };

                                await completion.Invoke((mapBody, [shot]));
                             
                                if (yPosition % 2 == 0)
                                {
                                    xPosition++;

                                    if ((xPosition * fovCol) + 1 > maxCol)
                                    {
                                        xPosition--;
                                        yPosition++;
                                    }
                                }
                                else
                                {
                                    xPosition--;

                                    if (xPosition < 0)
                                    {
                                        xPosition = 0;
                                        yPosition++;
                                    }
                                }
                            }
                            catch(OperationCanceledException cancelEx)
                            {
                                GlobalErrorHandler.HandleException(cancelEx);
                                throw;
                            }
                        }
                    }
                    break;
                
                case SW_PACKET:
                    if (camera.cameraTriggerMode == ECameraTriggerMode.SoftwareTrigger || true)
                    {
                        int xPosition = (int)request.MapBody!.XPosition;
                        int yPosition = (int)request.MapBody!.YPosition;

                        var shots = CameraManager.Instance.RetrieveMultiShots(ECamera.Mapping);
                        MapResponse capDoneResponse = new MapResponse
                        {
                            CommonBody = request.CommonBody!,
                            InspectionResult = 2,
                            ErrorType = 0,
                            XOffset = 0,
                            YOffset = 0,
                            TOffset = 0,
                            MapBody = request.MapBody
                        };

                        capDoneResponse.CommonBody.DataLength = 60;
                        capDoneResponse.CommonBody.CommonHeader = 0x01;

                        log.Info($"[Response] Cap done ({capDoneResponse})");
                        Respond(capDoneResponse);

                        await completion.Invoke((request.MapBody, shots));
                    }
                    break;

                case TEST_PACKET:
                    GVisionMessenger.Instance.UI.SendMappingUIUpdate(EMoldInspectionUIUpdateType.ClearAllResults);
                    deviceCoordinateServce.CalculateNTotalFov(out int totalYFov, out int totalXFov);

                    var totalShotNo = totalXFov * totalYFov;

                    var fovColSize = deviceCoordinateServce.FOVSize.Col;
                    var fovRowSize = deviceCoordinateServce.FOVSize.Row;

                    var maxColSize = deviceCoordinateServce.VisionTableSize.Col;
                    var maxRowSize = deviceCoordinateServce.VisionTableSize.Row;

                    int startXPosition = 0;
                    int startYPosition = 0;
                 
                    for (int i = 0; i < totalShotNo; i++)
                    {
                        try
                        {
                            var localShot = LocalMappingImageQueue.Instance.Dequeue(cancellationToken); // CameraManager.Instance.RetrieveImage(ECamera.Mapping);

                            var mapBody = new MapBody
                            {
                                StripBarcode = request.MapBody.StripBarcode,
                                Sequence = request.MapBody.Sequence,
                                GridTableNum = request.MapBody.GridTableNum,
                                XPosition = (uint)startXPosition,
                                YPosition = (uint)startYPosition
                            };

                            await completion.Invoke((mapBody, [localShot]));

                            if (startYPosition % 2 == 0)
                            {
                                startXPosition++;

                                if ((startXPosition * fovColSize) + 1 > maxColSize)
                                {
                                    startXPosition--;
                                    startYPosition++;
                                }
                            }
                            else
                            {
                                startXPosition--;

                                if (startXPosition < 0)
                                {
                                    startXPosition = 0;
                                    startYPosition++;
                                }
                            }
                        }
                        catch(OperationCanceledException cancelEx)
                        {
                            GlobalErrorHandler.HandleException(cancelEx);
                            throw;
                        }
                    }
                    break;
                    

                default:
                    break;
            }
        }

        private void saveMultiShotImage(List<InspectionResult> results, int xPosition, int yPosition, out List<string> imagePaths)
        {
            imagePaths = new List<string>();

            foreach (InspectionResult result in results)
            {
                ESaveOption saveOption = GlobalSetting.Instance.Inspection.SaveOption;

                EResultType resultType = result.ErrorType();

                if (saveOption == ESaveOption.NoSave) { continue; }
                if (saveOption == ESaveOption.Fail && resultType == EResultType.Good) { continue; }
                if (saveOption == ESaveOption.FailWithoutXOut)
                {
                    if (resultType == EResultType.Good || resultType == EResultType.XOut || resultType == EResultType.XOut2)
                        continue;
                }

                for (int index = 0; index < result.Shots.Count; index++)
                {
                    HObject? img = result.Shots[index];
                    SaveImage(img, this.inspectionType, resultType, xPosition, yPosition, index, out string? imagePath);
                    imagePaths.Add(imagePath!);

                    SaveDeepLearningImage(img, this.inspectionType, result.ErrorType(), xPosition, yPosition, index);
                }
            }
        }
    }
}