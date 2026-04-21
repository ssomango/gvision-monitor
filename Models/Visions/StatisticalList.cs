namespace GVisionWpf.Models.Visions
{
    public class StatisticalList<T> where T : IStatistical<T>
    {
        private readonly List<T> items = new List<T>();

        public int Count => this.items.Count;

        public T this[int index]
        {
            get => this.items[index];
            set => this.items[index] = value;
        }

        public StatisticalList() { }

        public StatisticalList(List<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            this.items.AddRange(list);
        }

        public void Add(T item)
        {
            this.items.Add(item);
        }

        public T MemberwiseMin()
        {
            return this.items.First().MemberWiseMin(this.items);
        }

        public T MemberwiseAverage()
        {
            return this.items.First().MemberWiseAverage(this.items);
        }

        public T MemberwiseMax()
        {
            return this.items.First().MemberWiseMax(this.items);
        }

        public override string ToString()
        {
            if (this.items.Count == 0)
            {
                return "Empty";
            }

            return $"Min({MemberwiseMin()}), " +
                   $"Avg({MemberwiseAverage()}), " +
                   $"Max({MemberwiseMax()}), " +
                   $"{this.items.Count}EA";
        }
    }
}