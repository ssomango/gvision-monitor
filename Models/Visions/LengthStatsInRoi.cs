namespace GVisionWpf.Models.Visions
{
    public class LengthStatsInRoi
    {
        private readonly string roiName;
        public StatisticalList<Length> Items = new StatisticalList<Length>();

        public LengthStatsInRoi(string roiName)
        {
            this.roiName = roiName;
        }

        public void Add(Length item)
        {
            this.Items.Add(item);
        }

        public Length MemberwiseMin()
        {
            return this.Items.MemberwiseMin();
        }

        public Length MemberwiseMax()
        {
            return this.Items.MemberwiseMax();
        }

        public Length MemberwiseAverage()
        {
            return this.Items.MemberwiseAverage();
        }

        public override string ToString()
        {
            return $"{this.roiName} ({this.Items})";
        }
    }
}