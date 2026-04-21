using GVisionWpf.Cameras;
using GVisionWpf.DomainLayer.Services.Running;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Dtos.Response;
using GVisionWpf.Models.Entities.Result;
using log4net;

namespace GVisionWpf.PresentationLayer.Controllers
{
    public class StripController : BaseController
    {
        private static readonly Lazy<StripController> lazy = new Lazy<StripController>(() => new StripController());
        public static StripController Instance => lazy.Value;

        private StripInspectionHandler inspectionHander = new StripInspectionHandler();

        private readonly TaskQueue codeTaskQueue;

        private static readonly ILog log = LogManager.GetLogger("STRIP");

        private const string TASK_NAME = "StripBarcodeInspection";

        private StripController()
        {
            this.codeTaskQueue = new TaskQueue(8);
        }

        public async void StripInspection(StripBarcodeRequest stripRequest)
        {
            log.Info($"[Request] 2D Barcode ({stripRequest})");

            ERequestInspectionType inspectionType = (ERequestInspectionType)stripRequest.CommonBody.InspectionType;

            ECamera cameraType = GlobalSetting.Instance.ECameraNos[stripRequest.CommonBody!.CameraId];

            List<HObject> images = CameraManager.Instance.RetrieveMultiShots(cameraType);
            HOperatorSet.CopyImage(images.First(), out HObject image);

            for (int i = 1; i < images.Count(); i++)
            {
                HOperatorSet.AddImage(image, images[i], out image, 0.5, 0);
                images[i].Dispose();
            }

            // 기존 Visio에는 BottomBarCode에서만 TurnOffAllLights 실행 중
            LightManager.Instance.TurnOffAllLights(cameraType);

            this.codeTaskQueue.EnqueueTask(async (cancellationToken) =>
            {
                InspectionResult result = await inspectionHander.Inspect(images.First(), cameraType, inspectionType, stripRequest);

                if (result is StripInspectionResult stripResult)
                {
                    if (stripResult.StripDataCode?.Value != null)
                    {
                        StripBarcodeResponse response = new StripBarcodeResponse
                        {
                            CommonBody = stripRequest.CommonBody!,
                            InspectionResult = stripResult.StripDataCode.Type == EResultType.Good ? 1u : 0u,
                            ErrorType = 0,
                            StripBody = new StripBody
                            {
                                StripBarcode = new char[128],
                                StripCount = stripRequest.StripBody.StripCount
                            },
                        };

                        if (stripResult is
                            {
                                XOffset: { Type: EResultType.Good, Value: var xValue },
                                YOffset: { Type: EResultType.Good, Value: var yValue }
                            })
                        {
                            response.XOffset = xValue;
                            response.YOffset = yValue;
                        }

                        response.CommonBody.DataLength = 168;
                        response.CommonBody.CommonHeader = 1;

                        stripResult.StripDataCode.Value!.ToCharArray().CopyTo(response.StripBody.StripBarcode, 0);

                        Respond(response);
                        log.Info($"[Response] 2D Barcode ({response})");
                    }
                    else
                    {
                        GVisionMessenger.Instance.UI.SendSystemInfoMessage("Strip Barcode decoding failed (Value is null).");
                        log.Info("Strip Barcode decoding failed (Value is null)");
                    }
           
                    SaveImage(image, EInspection.DataCode, EResultType.Good, 99, 99, null, out string? imagePath);    
                }
            });
        }
    }
}