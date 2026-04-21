using System.Threading;
using GVisionWpf.Exceptions;
using Microsoft.VisualBasic.Logging;

namespace GVisionWpf.Cameras.CamearaQueues
{
    public class LocalPrsImageQueue : ImageQueue
    {
        private static readonly Lazy<LocalPrsImageQueue> lazy = new Lazy<LocalPrsImageQueue>(() => new LocalPrsImageQueue());
        public static LocalPrsImageQueue Instance => lazy.Value;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(8, 8); // 최대 8개까지 저장 가능

        private LocalPrsImageQueue() { }

        public override void Enqueue(HObject image)
        {
            this.semaphore.Wait();
            this.Queue.Add(image);
        }

        public override HObject Dequeue()
        {
            var image = this.Queue.Take();
            this.semaphore.Release();
            return image;
        }

        public override HObject Dequeue(int timeoutMs)
        {
            if (!this.Queue.TryTake(out HObject image, timeoutMs))
            {
                this.semaphore.Release();
                throw new CameraTimeOutException();
            }

            this.semaphore.Release();
            return image;

        }

        public override HObject Dequeue(CancellationToken cancellationToken)
        {
            var image = Queue.Take(cancellationToken);

            this.semaphore.Release();

            return image;
        }

        public override int Count()
        {
            return this.Queue.Count;
        }

        public override void Clear()
        {
            while (this.Queue.TryTake(out _)) { }
            this.semaphore.Release(8 - this.semaphore.CurrentCount);
        }
    }
}
