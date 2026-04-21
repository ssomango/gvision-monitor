

namespace GVisionWpf.Events
{
    public sealed class ItemEventArgs<T> : EventArgs
    {
        public T Item { get; }

        public ItemEventArgs(T item)
        {
            Item = item;
        }
    }
}
