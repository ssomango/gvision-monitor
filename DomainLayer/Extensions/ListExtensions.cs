using GVisionWpf.DomainLayer.Interfaces;

namespace GVisionWpf.DomainLayer.Extensions
{
    public static class ListExtensions
    {
        public static void CopyFrom<T>(this IList<T> targetList, IList<T> sourceList)
            where T : class, ICopyable<T>
        {
            if (targetList == null || sourceList == null) throw new ArgumentNullException();

            targetList.Clear();

            foreach (var source in sourceList)
            {
                targetList.Add(source);
            }
        }
    }
}
