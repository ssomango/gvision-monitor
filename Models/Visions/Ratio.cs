namespace GVisionWpf.Models.Visions
{
    public struct Ratio : IComparable<Ratio>, IStatistical<Ratio>
    {
        public int Value { get; set; }

        public Ratio(int value)
        {
            Value = value;
        }

        public Ratio(double value)
        {
            Value = (int)value;
        }

        public override string ToString()
        {
            return $"{Value}%";
        }

        public int CompareTo(Ratio other)
        {
            return Value.CompareTo(other.Value);
        }

        public Ratio MemberWiseMin(List<Ratio> list)
        {
            return new Ratio(list.Min(r => r.Value));
        }

        public Ratio MemberWiseMax(List<Ratio> list)
        {
            return new Ratio(list.Max(r => r.Value));
        }

        public Ratio MemberWiseAverage(List<Ratio> list)
        {
            return new Ratio(list.Average(r => r.Value));
        }
    }
}