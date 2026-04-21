using System;
using System.Threading;
using System.Windows.Threading;

namespace GVisionWpf.Utils
{
    public sealed class Debouncer : IDisposable
    {
        private readonly object locked = new object();
        private readonly Dispatcher dispatcher;
        private Timer? timer;

        public Debouncer(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void Debounce(int delayMilliseconds, Action action)
        {
            if (delayMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(delayMilliseconds));
            if (action is null) throw new ArgumentNullException(nameof(action));

            lock (this.locked)
            {
                this.timer?.Dispose();
                this.timer = new Timer(_ =>
                {
                    lock (this.locked)
                    {
                        this.timer?.Dispose();
                        this.timer = null;
                    }

                    if (this.dispatcher.CheckAccess())
                    {
                        action();
                    }
                    else
                    {
                        _ = this.dispatcher.BeginInvoke(action);
                    }

                }, state: null, dueTime: delayMilliseconds, period: Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            lock (this.locked)
            {
                this.timer?.Dispose();
                this.timer = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}