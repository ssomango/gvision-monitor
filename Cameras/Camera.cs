using GVisionWpf.Exceptions;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.UIs.ViewModels;
using GVisionWpf.Visions;
using log4net;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.Cameras
{
    public abstract class Camera : CameraObservable
    {
        
        public ECameraMode CameraMode = ECameraMode.None;
        public ECameraTriggerMode cameraTriggerMode = ECameraTriggerMode.HardwareTrigger;
        public double PixelPerMillimeter;
        public HTuple FrameGrabberHandle = new HTuple();
        private CancellationTokenSource? cancellationTokenSource;
        private CancellationTokenSource? triggerCancellationTokenSource;
        private readonly Object lockObject = new object();
        private readonly Object previousFrameLockObject = new object();
        private HObject? previousFrameImage;
        public bool IsHorizontalFlip;
        public bool IsVerticalFlip;
        protected readonly int MaxDelay;

        protected abstract void SetCameraModeConcrete(ECameraMode cameraMode);
        public abstract void AbortGrab();
        public abstract void TriggerShot();

        private static readonly ILog log = LogManager.GetLogger("Camera");

        public HObject? PreviousFrameImage
        {
            get
            {
                lock (this.previousFrameLockObject)
                {
                    return this.previousFrameImage;
                }
            }
            set
            {
                lock (this.previousFrameLockObject)
                {
                    this.previousFrameImage?.Dispose();
                    this.previousFrameImage = value;
                }
            }
        }

        protected Camera(ECamera cameraType, double pixelPerMillimeter, bool isHorizontalFlip, bool isVerticalFlip, int maxDelay = -1)
        {
            this.CameraType = cameraType;
            this.PixelPerMillimeter = pixelPerMillimeter;
            this.IsHorizontalFlip = isHorizontalFlip;
            this.IsVerticalFlip = isVerticalFlip;
            this.MaxDelay = maxDelay;
        }

        ~Camera()
        {
            ReleaseFrameGrabber();
        }

        public HObject RetrieveImage()
        {
            lock (this.lockObject)
            {
                if (!IsInitialized())
                {
                    throw new NotInitializedCameraException();
                }

                HOperatorSet.GrabImageAsync(out HObject image, this.FrameGrabberHandle, this.MaxDelay);

                image = this.flipImageByCameraSetting(image);
                return image;
            }
        }

        public HObject RetrieveImageSync()
        {
            lock (this.lockObject)
            {
                if (!IsInitialized())
                {
                    throw new NotInitializedCameraException();
                }

                HOperatorSet.GrabImage(out HObject image, this.FrameGrabberHandle);

                image = this.flipImageByCameraSetting(image);
                return image;
            }
        }

        public HObject flipImageByCameraSetting(HObject image)
        {
            if (this.IsHorizontalFlip)
            {
                VisionOperation.FlipImageHorizontally(image, out image);
            }

            if (this.IsVerticalFlip)
            {
                VisionOperation.FlipImageVertically(image, out image);
            }

            return image;
        }

        public void SetCameraMode(ECameraMode cameraMode)
        {
            lock (this.lockObject)
            {
                SetCameraModeConcrete(cameraMode);
            }
        }

        public Task Execute(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(40);

                    if (this.CameraMode == ECameraMode.HardwareTrigger)
                    {
                        this.TriggerShot();
                    }

                    HObject image = CameraManager.Instance.RetrieveImage(CameraType);
                    NotifyLiveObservers(image);

                    PreviousFrameImage = image;
                }
            }
            catch (Exception? ex)
            {
                this.ClearLiveObservers();
                GlobalErrorHandler.HandleException(ex);
            }

            return Task.FromResult(Task.CompletedTask);
        }

        public Task ExecuteMapTriggerListening(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    HObject image = CameraManager.Instance.RetrieveImage(CameraType);
                    NotifyTriggerObservers(image);
                    log.Info("Map Image Captured by Trigger");
                }
                catch (Exception? ex)
                {
                    GlobalErrorHandler.HandleException(ex);
                }
            }

            return Task.FromResult(Task.CompletedTask);
        }

        public Task ExecutePrsTriggerListening(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    HObject image = CameraManager.Instance.RetrieveImage(CameraType);
                    NotifyTriggerObservers(image);
                    log.Info("PRS Image Captured by Trigger");
                }
                catch (Exception? ex)
                {
                    GlobalErrorHandler.HandleException(ex);
                }
            }

            return Task.FromResult(Task.CompletedTask);
        }

        public override void StartLiveSource()
        {
            StopLiveSource();
            this.cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Execute(this.cancellationTokenSource.Token), this.cancellationTokenSource.Token);
        }

        protected override void StopLiveSource()
        {
            this.cancellationTokenSource?.Cancel();
        }

        public override void StartListeningPrsTrigger()
        {
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Start Listening PRS Trigger");
            this.triggerCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ExecutePrsTriggerListening(this.triggerCancellationTokenSource.Token), this.triggerCancellationTokenSource.Token);
        }


        public override void StartListeningMapTrigger()
        {
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Start Listening Mapping Trigger");
            this.triggerCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ExecuteMapTriggerListening(this.triggerCancellationTokenSource.Token), this.triggerCancellationTokenSource.Token);
        }

        public override void StopListeningPrsTrigger()
        {
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Stop Listening PRS Trigger");
            if(this.CameraMode == ECameraMode.HardwareTrigger)
                this.TriggerShot();
            this.triggerCancellationTokenSource?.Cancel();
        }

        public override void StopListeningMappingTrigger()
        {
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Stop Listening Mapping Trigger");
            if (this.CameraMode == ECameraMode.HardwareTrigger)
                this.TriggerShot();
            this.triggerCancellationTokenSource?.Cancel();
        }



        public void ReleaseFrameGrabber()
        {
            lock (this.lockObject)
            {
                if (!IsInitialized())
                {
                    return;
                }

                HOperatorSet.CloseFramegrabber(this.FrameGrabberHandle);
            }
        }

        public bool IsInitialized()
        {
            return this.FrameGrabberHandle.Length != 0;
        }
    }
}