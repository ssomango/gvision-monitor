using System.Collections.Concurrent;

namespace GVisionWpf.Utils
{
    public class DisposeBag : IDisposable
    {
        private readonly ConcurrentBag<IDisposable> disposables = new ConcurrentBag<IDisposable>();

        public int Count => disposables.Count;

        public bool IsEmpty => disposables.Count == 0;

        public void Add(IDisposable disposable)
        {
            if (disposable != null)
            {
                disposables.Add(disposable);
            }
        }

        public void Add(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                this.disposables.Add(disposable);
            }
        }

        public void Add(IEnumerable<IDisposable> disposables)
        {
            foreach (var disposable in disposables)
            {
                this.disposables.Add(disposable);
            }
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                if (disposables == null) continue;

                disposable.Dispose();
            }

            disposables?.Clear();
        }

        public void Clear() => disposables.Clear();
    }
}
