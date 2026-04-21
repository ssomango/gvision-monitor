
namespace GVisionWpf.Cameras
{
    public abstract class TriggerObservable
    {
        public ECamera CameraType { get; set; }

        private readonly object lockObject = new object();
        private readonly List<ITriggerObserver> triggerObservers = new List<ITriggerObserver>();

        public void AddObserver(ITriggerObserver observer)
        {
            lock (this.lockObject)
            {
                this.triggerObservers.Add(observer);
            }
        }

        public void RemoveObserver(ITriggerObserver observer)
        {
            lock (this.lockObject)
            {
                this.triggerObservers.Remove(observer);
            }
        }

        // TODO: 이름 적절하게 생각나면 바꾸기
        public void ClearObservers()
        {
            lock (this.lockObject)
            {
                this.triggerObservers.Clear();
                StopListeningTrigger();
            }
        }

        protected void NotifyObservers(HObject image)
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

        public abstract void StartListeningTrigger();
        public abstract void StopListeningTrigger();
    }
}