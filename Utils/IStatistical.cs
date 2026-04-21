namespace GVisionWpf.Utils
{
    public interface IStatistical<T>
    {
        public T MemberWiseMin(List<T> list);
        public T MemberWiseMax(List<T> list);
        public T MemberWiseAverage(List<T> list);
    }
}