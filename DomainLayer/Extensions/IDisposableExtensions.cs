namespace GVisionWpf.DomainLayer.Extensions
{
    public static class IDisposableExtensions
    {
        public static T DisposeBy<T>(this T disposable, DisposeBag bag) where T : IDisposable
        {
            bag.Add(disposable);
            return disposable;
        }
    }
}
