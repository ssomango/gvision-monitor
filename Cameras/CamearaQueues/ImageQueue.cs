using System.Collections.Concurrent;
using System.Threading;

namespace GVisionWpf.Cameras.CamearaQueues
{
    public abstract class ImageQueue : ITriggerObserver
    {
        protected BlockingCollection<HObject> Queue = new BlockingCollection<HObject>();

        public void Update(HObject image)
        {
            Enqueue(image);
        }

        public abstract void Enqueue(HObject image);

        public abstract HObject Dequeue();

        public abstract HObject Dequeue(int timeoutMs);

        public abstract HObject Dequeue(CancellationToken cancellationToken);

        public abstract int Count();

        public abstract void Clear();
    }
}
