namespace GVisionWpf.DomainLayer.Interfaces
{
    public interface ICopyable<T> where T : class
    {
        public void CopyFrom(T other);
    }
}
