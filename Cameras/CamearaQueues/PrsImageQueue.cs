using System.Threading;
using GVisionWpf.Exceptions;
using Microsoft.VisualBasic.Logging;

namespace GVisionWpf.Cameras.CamearaQueues
{
    public class PrsImageQueue : ImageQueue
    {
        private static readonly Lazy<PrsImageQueue> lazy = new Lazy<PrsImageQueue>(() => new PrsImageQueue());
        public static PrsImageQueue Instance => lazy.Value;

        private PrsImageQueue() { }

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
            // 뾱뾱뾱뾱뾱ㄱ
            while (this.Queue.TryTake(out _)) { }
        }
    }
}
