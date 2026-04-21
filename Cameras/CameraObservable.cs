using GVisionWpf.Illuminations;


namespace GVisionWpf.Cameras
{
    public abstract class CameraObservable
    {
        public ECamera CameraType { get; set; }

        private readonly object liveLockObject = new object();
        public readonly List<ILiveObserver> liveObservers = new List<ILiveObserver>();

        private readonly object lockObject = new object();
        private readonly List<ITriggerObserver> triggerObservers = new List<ITriggerObserver>();

        public void AddLiveObserver(ILiveObserver observer)
        {
            lock (this.liveLockObject)
            {
                this.liveObservers.Add(observer);

                if (this.liveObservers.Count == 1)
                {
                    LightManager.Instance.TurnOnLight(CameraType);
                    StartLiveSource();
                }
            }
        }

        public void RemoveLiveObserver(ILiveObserver observer)
        {
            lock (this.liveLockObject)
            {
                this.liveObservers.Remove(observer);

                if (this.liveObservers.Count == 0)
                {
                    StopLiveSource();
                    LightManager.Instance.TurnOffAllLights(CameraType);
                }
            }
        }

        // TODO: 이름 적절하게 생각나면 바꾸기
        public void ClearLiveObservers()
        {
            lock (this.liveLockObject)
            {
                this.liveObservers.Clear();
                StopLiveSource();
            }
        }


        public void AddTriggerObserver(ITriggerObserver observer)
        {
            lock (this.lockObject)
            {
                this.triggerObservers.Add(observer);
            }
        }

        public void RemoveTriggerObserver(ITriggerObserver observer)
        {
            lock (this.lockObject)
            {
                this.triggerObservers.Remove(observer);
            }
        }

        public void ClearTriggerObservers()
        {
            lock (this.lockObject)
            {
                this.triggerObservers.Clear();
            }
        }

        public int GetCountLiveObservers() => this.liveObservers.Count;

        protected void NotifyTriggerObservers(HObject image)
        {
            List<ITriggerObserver> observersSnapshot;
            lock (this.lockObject)
            {
                // Take a snapshot of the triggerObservers list to avoid locking during notification
                observersSnapshot = new List<ITriggerObserver>(this.triggerObservers);
            }

            foreach (ITriggerObserver observer in observersSnapshot)
            {
                observer.Update(image);
            }
        }


        protected void NotifyLiveObservers(HObject image)
        {
            List<ILiveObserver> observersSnapshot;
            lock (this.liveLockObject)
            {
                // Take a snapshot of the liveObservers list to avoid locking during notification
                observersSnapshot = new List<ILiveObserver>(this.liveObservers);
            }

            foreach (ILiveObserver observer in observersSnapshot)
            {
                observer.UpdateFrame(image);
            }
        }

        public abstract void StartListeningPrsTrigger();
        public abstract void StartListeningMapTrigger();
        public abstract void StopListeningPrsTrigger();
        public abstract void StopListeningMappingTrigger();
        public abstract void StartLiveSource();
        protected abstract void StopLiveSource();
    }
}