using GVisionWpf.Cameras;
using GVisionWpf.DSMMI.Dto;
using GVisionWpf.DSMMI.Inspection;
using GVisionWpf.DSMMI.UI;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.UiModels;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.UIs.ViewModels;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GVisionWpf.DSMMI.Frames
{
    /// <summary>
    /// VinsFantasyWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class VinsFantasyWindow : Window
    {
        private CancellationTokenSource prsCancellationTokenSource;
        private CancellationTokenSource mappingCancellationTokenSource;
        private CancellationTokenSource? triggerCancellationTokenSource;

        public VinsFantasyWindow()
        {
            InitializeComponent();
            DataContext = VinsFantasyViewModel.Instance;

            Application.Current.Dispatcher.Invoke(() =>
            {
                VinsFantasyViewModel.Instance.ScrollToNewItemRequested += (_, item) =>
                {
                    this.listView.ScrollIntoView(item);
                };
            });

            VinsCommunicator.Instance.InitializeClient();
            //CameraManager.Instance.SetFileCameraMode();
            CameraManager.Instance.SetRunMode();
        }

        private void lotStart_OnClick(object sender, RoutedEventArgs e)
        {
            InputTextWindow inputTextWindow = new InputTextWindow("Enter the Lot Number", "Lot Number");

            if (inputTextWindow.ShowDialog() != true) { return; }

            string lotNumber = inputTextWindow.xTextBox.Text;

            try
            {
                File.WriteAllText(GlobalSetting.Instance.DeviceInfo.CurLotPath, lotNumber);
            }
            catch (Exception ex)
            {
                new AlertWindow("LOT start error", "캄 다운!", AlertWindow.EAlert.YES).ShowDialog();
                return;
            }

            LotRequest lotRequest = new LotRequest() { CommonBody = new Models.Dtos.Common.CommonBody() { CommonHeader = 0x00010004, CameraId = 255, InspectionType = 9999 } };
            VinsCommunicator.Instance.Send(lotRequest);

            // GVisionMessenger.Instance.UI.SendSystemInfoMessage("[LOT] LOT start: " + lotNumber);
        }

        private void lotEnd_OnClick(object sender, RoutedEventArgs e)
        {
            LotRequest lotRequest = new LotRequest() { CommonBody = new Models.Dtos.Common.CommonBody() { CommonHeader = 0x00010007, CameraId = 255, InspectionType = 9999 } };
            VinsCommunicator.Instance.Send(lotRequest);

            // GVisionMessenger.Instance.UI.SendSystemInfoMessage("[LOT] LOT end");
        }

        private void recipeChangeButton_OnClick(object sender, RoutedEventArgs e)
        {
            RecipeRequest recipeRequest = new RecipeRequest() { CommonBody = new CommonBody() { CommonHeader = 0x00010005, CameraId = 255, InspectionType = 9999 } };
            VinsCommunicator.Instance.Send(recipeRequest);
        }

        public Task ExecuteTriggerSaving(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    HObject image = CameraManager.Instance.RetrieveImage(ECamera.PRS);
                    DateTime today = DateTime.Now;
                    String date = today.ToString("yyyy-MM-dd");
                    String time = today.ToString("HHmmssfff");
                    String fileType = ".png";
                    String baseDirectory = "DB/Images";
                    String fullDirectory = Path.Combine(new string[] { baseDirectory, date, "Manual" });
                    String fileName = time + "-" + fileType;
                    String path = fullDirectory + "/" + fileName;

                    Directory.CreateDirectory(fullDirectory);
                    _ = Task.Run(() => { HOperatorSet.WriteImage(image, "png fastest", 0, path); });
                }
            }
            catch (Exception? ex)
            {
                GlobalErrorHandler.HandleException(ex);
            }

            return Task.FromResult(Task.CompletedTask);
        }

        private void stopSavingTriggeredImage()
        {
            if (this.triggerCancellationTokenSource == null)
            {
                return;
            }

            this.triggerCancellationTokenSource?.Cancel();

            if (CameraManager.Instance.Cameras[ECamera.PRS].CameraMode == ECameraMode.HardwareTrigger)
            {
                CameraManager.Instance.Cameras[ECamera.PRS].TriggerShot();
            }
        }

        private void triggerSaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if ((string)this.xTriggerSaveButton.Content == "TRIG SAVE ON")
            {
                this.xTriggerSaveButton.Content = "TRIG SAVE OFF";
                this.triggerCancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ExecuteTriggerSaving(this.triggerCancellationTokenSource.Token), this.triggerCancellationTokenSource.Token);
                VinsFantasyViewModel.Instance.Print("[TRIG MAN] All triggered images will be saved in DB/Images/yyyy-MM-dd/Manual (PRS Only)");
            }
            else
            {
                this.stopSavingTriggeredImage();
                VinsFantasyViewModel.Instance.Print("[TRIG MAN] Stop saving images. (PRS Only)");
                this.xTriggerSaveButton.Content = "TRIG SAVE ON";
            }
        }

        private async void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            StopMapping();
            StopPrs();
            this.stopSavingTriggeredImage();

            await Task.Delay(300);

            VinsCommunicator.Instance.ReleaseClient();
            CameraManager.Instance.SetRealCameraMode();
            CameraManager.Instance.SetRunMode();
            this.Close();
        }

        private async void autoRun_OnClick(object sender, RoutedEventArgs e)
        {
            if ((string)this.xAutoRunOnOffButton.Content == "AR ON")
            {
                this.xAutoRunOnOffButton.Content = "AR OFF";
                await StartAutoRun();
            }
            else
            {
                StopAutoRun();
                this.xAutoRunOnOffButton.Content = "AR ON";
            }
        }

        private async void mapping_OnClick(object sender, RoutedEventArgs e)
        {
            if ((string)this.xMappingOnOffButton.Content == "MAPPING ON")
            {
                this.xMappingOnOffButton.Content = "MAPPING OFF";
                await StartMapping();
            }
            else
            {
                StopMapping();
                this.xMappingOnOffButton.Content = "MAPPING ON";
            }

        }

        private async void prs_OnClick(object sender, RoutedEventArgs e)
        {
            if ((string)this.xPrsOnOffButton.Content == "PRS ON")
            {
                this.xPrsOnOffButton.Content = "PRS OFF";
                await StartPrs();
            }
            else
            {
                StopPrs();
                this.xPrsOnOffButton.Content = "PRS ON";
            }
        }

        private async void futureUse_OnClick(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                new AlertWindow("Notification", "Do you need a new feature on this button? Please find betterjeong!", AlertWindow.EAlert.YESNO).ShowDialog();
            });
        }

        private async Task StartPrs()
        {
            this.prsCancellationTokenSource = new CancellationTokenSource();

            GVisionMessenger.Instance.UI.SendSystemInfoMessage("[AUTO RUN] Started Auto Run Mode (PRS Only)");

            await Prs.Instance.Run(this.prsCancellationTokenSource.Token);
        }

        private void StopPrs()
        {
            if (this.prsCancellationTokenSource == null)
            {
                return;
            }

            this.prsCancellationTokenSource.Cancel();
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("[AUTO RUN] Stopped Auto Run Mode (PRS Only)");
        }

        private async Task StartMapping()
        {
            this.mappingCancellationTokenSource = new CancellationTokenSource();

            GVisionMessenger.Instance.UI.SendSystemInfoMessage("[AUTO RUN] Started Auto Run Mode (Mapping Only)");

            //sendEmapRequest();


            await Mapping.Instance.Run(this.mappingCancellationTokenSource.Token);
        }


        private void StopMapping()
        {
            if (this.mappingCancellationTokenSource == null)
            {
                return;
            }

            this.mappingCancellationTokenSource.Cancel();
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("[AUTO RUN] Stopped Auto Run Mode (Mapping Only)");
        }

        private async Task StartAutoRun()
        {
            this.prsCancellationTokenSource = new CancellationTokenSource();
            this.mappingCancellationTokenSource = new CancellationTokenSource();

            GVisionMessenger.Instance.UI.SendSystemInfoMessage("[AUTO RUN] Started Auto Run Mode (Mapping + PRS)");
            int seq = 0;

            while (true)
            {
                seq++;
                var prsTask = Task.Run(async () => { await Prs.Instance.Run(this.prsCancellationTokenSource.Token); });
                var mappingTask = Task.Run(async () => { await Mapping.Instance.Run(this.mappingCancellationTokenSource.Token); });
                await Task.WhenAll(mappingTask, prsTask);
            }
        }

        private void StopAutoRun()
        {
            this.prsCancellationTokenSource.Cancel();
            this.mappingCancellationTokenSource.Cancel();
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("[AUTO RUN] Stopped Auto Run Mode (Mapping + PRS)");
        }

        private void emap_Onclick(object sender, RoutedEventArgs e)
        {
            sendEmapRequest();
        }

        private void sendEmapRequest()
        {
            //uint tableRand = (uint)new Random().Next(0, 2);
            uint tableRand = 0;

            TableLayout trayLayout = MapDeviceViewViewModel.Instance.VisionTableLayout;

            int xMax = trayLayout.Col;
            int yMax = trayLayout.Row;
            int maxCellCount = Math.Min(xMax * yMax, 128);
            int randCount = new Random().Next(0, maxCellCount);

            List<(uint, uint)> randPosition = getRandomIndexList(xMax, yMax, randCount);


            List<EachEmapBody> emaps = new List<EachEmapBody>();

            foreach (var i in randPosition)
            {
                emaps.Add(new EachEmapBody
                {
                    XPickPosition = i.Item1,
                    YPickPosition = i.Item2,
                    Data = 4,
                    Dummy = 0
                });
            }

            EmapRequest emapRequest = new EmapRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xffffffff,
                    CommonHeader = 0x00010009,
                },
                TriggerType = 0x00,
                CaptureDone = 0x00,
                Sequence = 0x00,
                GridTableNumber = tableRand,
                EmapBodies = emaps
            };

            VinsCommunicator.Instance.Send(emapRequest);
        }

        private List<(uint, uint)> getRandomIndexList(int xMax, int yMax, int count)
        {
            Random random = new Random();
            HashSet<(uint, uint)> selectedSet = new HashSet<(uint, uint)>();

            while (selectedSet.Count < count)
            {
                uint randomX = (uint)random.Next(0, xMax);
                uint randomY = (uint)random.Next(0, yMax);

                selectedSet.Add((randomX, randomY));
            }

            return new List<(uint, uint)>(selectedSet);
        }
    }
}
