using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos;
using GVisionWpf.PresentationLayer.Communications;
using System.IO;

namespace GVisionWpf.PresentationLayer.Controllers
{
    public abstract class BaseController
    {
        private readonly Communicator communicator = Communicator.Instance;

        private readonly TaskQueue imageSavingTaskQueue = new TaskQueue(2);

        public void Respond(IBytesConvertible response)
        {
            this.communicator.Send(response);
        }

        public void ThrowUnlessAllowedInCurrentMode(string taskName)
        {
        }

        public void SaveImage(HObject image, EInspection inspection, EResultType resultType, int xPosition, int yPosition, int? shotNo, out string? path)
        {
            path = null;
            ESaveOption saveOption = GlobalSetting.Instance.Inspection.SaveOption;

            if (saveOption == ESaveOption.NoSave) { return; }
            if (saveOption == ESaveOption.Fail && resultType == EResultType.Good) { return; }
            if (saveOption == ESaveOption.FailWithoutXOut)
            {
                if (resultType == EResultType.Good || resultType == EResultType.XOut || resultType == EResultType.XOut2)
                    return;
            }

            DateTime today = DateTime.Now;

            string baseDirectory = "DB/Images";
            string date = today.ToString("yyyy-MM-dd");
            string lotNumber = GlobalSetting.Instance.DeviceInfo.LotNumber;
            string inspectionResult = (inspection.ToString() + "-" + resultType.ToString()).ToLower();
            string time = today.ToString("HHmmssfff");
            string position = "x" + xPosition.ToString() + "y" + yPosition.ToString();

            if (shotNo != null)
            {
                position += $"#{shotNo}";
            }

            string fileType = ".png";

            string fullDirectory = Path.Combine(new string[] { baseDirectory, date, lotNumber, inspectionResult });
            string fileName = time + "-" + position + fileType;

            path = fullDirectory + "/" + fileName;

            Directory.CreateDirectory(fullDirectory);
            this.imageSavingTaskQueue.EnqueueTask(async (cancellationToken) =>
            {
                HOperatorSet.WriteImage(image, "png fastest", 0, fullDirectory + "/" + fileName);
            });
        }

        public void InvertOffset<T>(T response, string taskName) where T : IHasXYOffset
        {
            if (!GlobalSetting.Instance.ControllerInfos.TryGetValue(taskName, out ControllerInfo? controllerInfo))
            {
                throw new WrongSettingException();
            }

            if (controllerInfo.InvertXOffset)
            {
                response.XOffset *= -1;
            }

            if (controllerInfo.InvertYOffset)
            {
                response.YOffset *= -1;
            }

            if (response is IHasXYTOffset tResponse)
            {
                bool invertTOffset = Convert.ToBoolean(controllerInfo.InvertTOffset);

                if (invertTOffset)
                {
                    tResponse.TOffset *= -1;
                }
            }
        }

        public void SetOffset<T>(T response, Pose offset, string taskName) where T : IHasXYOffset
        {
            LengthUnit lengthUnit = GlobalSetting.Instance.Inspection.LengthUnit;
            response.XOffset = (int)(offset.X * 1000 / lengthUnit.RelativeWeight);
            response.YOffset = (int)(offset.Y * 1000 / lengthUnit.RelativeWeight);

            if (response is IHasXYTOffset tResponse)
            {
                tResponse.TOffset = (int)(offset.T * 1000);
            }

            InvertOffset(response, taskName);
        }

        public void SaveDeepLearningImage(HObject image, EInspection inspection, EResultType resultType, int xPosition, int yPosition, int? shotNo)
        {
            return;

            if (GlobalSetting.Instance.SystemType != ESystemType.HanaMicron) { return; }

            if (resultType == EResultType.XOut || resultType == EResultType.XOut2) return;

            // Fail은 모두 저장, Good은 0.01% 확률로 저장
            if (resultType == EResultType.Good)
            {
                Random rand = new Random();
                double chance = rand.NextDouble(); // 0 ~ 1
                if (chance > 0.0001) // 0.01% 확률이면 0.0001
                {
                    return;
                }
            }

            if (image == null || !image.IsInitialized())
            {
                return;
            }

            DateTime now = DateTime.Now;

            // 기존 경로와 다르게 별도 경로 구성
            string baseDirectory = "\\\\192.168.10.150\\DeepLearning\\";
            string date = now.ToString("yyyy-MM-dd");
            string lotNumber = GlobalSetting.Instance.DeviceInfo.LotNumber;
            string inspectionResult = $"{inspection}-{resultType}".ToLower();
            string time = now.ToString("HHmmssfff");
            string position = $"x{xPosition}y{yPosition}";

            if (shotNo != null)
            {
                position += $"#{shotNo}";
            }

            string fileName = $"{time}-{position}.png";
            string fullDirectory = Path.Combine(baseDirectory, date, lotNumber, inspectionResult);

            String path = Path.Combine(fullDirectory, fileName);

            Directory.CreateDirectory(fullDirectory);

            this.imageSavingTaskQueue.EnqueueTask(async (cancellationToken) =>
            {
                try
                {

                    HOperatorSet.WriteImage(image, "png fastest", 0, path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DeepLearning] Image Save Failed: {ex.Message}");
                }
            });
        }


    }
}