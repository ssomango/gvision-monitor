using System.Threading;
using GVisionWpf.Exceptions;
using log4net;

namespace GVisionWpf.Cameras.CamearaQueues
{
    public class MappingImageQueue : ImageQueue
    {
        private static readonly Lazy<MappingImageQueue> lazy = new Lazy<MappingImageQueue>(() => new MappingImageQueue());
        public static MappingImageQueue Instance => lazy.Value;
        private MappingImageQueue() { }

        public override void Enqueue(HObject image)
        {
            this.Queue.Add(image);
        }

        public override HObject Dequeue()
        {
            return this.Queue.Take();
        }


        public override HObject Dequeue(int timeoutMs)
        {
            if (!this.Queue.TryTake(out HObject image, timeoutMs))
            {
                throw new CameraTimeOutException();
            }

            return image;
        }

        public override HObject Dequeue(CancellationToken cancellationToken) => Queue.Take(cancellationToken);

        public override int Count()
        {
            return this.Queue.Count;
        }

        public override void Clear()
        {
            while (this.Queue.TryTake(out _)) { }
        }
    }
}
