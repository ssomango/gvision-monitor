using GVisionWpf.Models.Dtos.Response;
using GVisionWpf.UIs.ViewModels;
using log4net;
using Microsoft.VisualBasic.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.PresentationLayer.Communications
{
    public class Heart
    {
        private static readonly Lazy<Heart> lazy = new Lazy<Heart>(() => new Heart());
        public static Heart Instance => lazy.Value;

        private readonly Communicator communicator = Communicator.Instance;

        private CancellationTokenSource? cancellationTokenSource;

        private static readonly ILog log = LogManager.GetLogger("Communication");

        // INTENTION: 매번 만들필요 없으니 그냥 재활용 합시다
        private readonly HeartBeatResponse heartBeatResponse = new HeartBeatResponse();
        private EHeartBeatMode heartBeatMode = EHeartBeatMode.Ready;
        private EVisionMode currentVisionMode = EVisionMode.Teaching;

        public EHeartBeatMode HeartBeatMode
        {
            get => this.heartBeatMode;
            set
            {
                this.heartBeatMode = value;
                // TODO: 상태에 따라 적절하게 heartBeatResponse 값 바꿔주기
                switch (this.heartBeatMode)
                {
                    case EHeartBeatMode.Ready:
                        Restart();
                        break;
                    case EHeartBeatMode.NotReady:
                    case EHeartBeatMode.Run:
                    case EHeartBeatMode.Paused:
                    case EHeartBeatMode.None:
                        Stop();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public EVisionMode CurrentVisionMode
        {
            get => currentVisionMode;
            set
            {
                if (currentVisionMode == value) return;

                currentVisionMode = value;

                heartBeatResponse.CurrentVisionMode = (uint)value;

                sendVisionModeChangedMessage(value);
            }
        }

        private void sendVisionModeChangedMessage(EVisionMode mode)
        {
            var visionModeChangedResponse = new HeartBeatResponse();
            visionModeChangedResponse.CurrentVisionMode = (uint)mode;
            visionModeChangedResponse.CurrentVisionStatus = 0x1B;

            communicator.Send(visionModeChangedResponse);

            GVisionMessenger.Instance.UI.SendSystemInfoMessage($"[Response] Send Changed Current Vision Mode - {mode}");
        }

        private Heart()
        {
            start();
        }

        private void start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Execute(this.cancellationTokenSource));
        }

        public void Stop()
        {
            this.cancellationTokenSource?.Cancel();
        }

        public void Restart()
        {
            Stop();
            start();
        }

        private void beat()
        {
            this.communicator.Send(this.heartBeatResponse);
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Heartbeat!");
        }

        public void StatusBeat()
        {
            this.communicator.Send(this.heartBeatResponse);
            log.Info($"[Response] Vision Status ({this.heartBeatResponse})");
        }

        public async Task Execute(CancellationTokenSource tokenSource)
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                beat();
                try
                {
                    await Task.Delay(10000, tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}