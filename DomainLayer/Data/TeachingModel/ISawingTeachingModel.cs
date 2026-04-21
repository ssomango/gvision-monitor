namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface ISawingTeachingModel<T> where T : InspectionTeaching
    {
        ICollection<SawOffsetItem> SawOffsetItems { get; set; }

        double OutlineWidth { get; set; }

        Threshold OutlineThreshold { get; set; }

        double MinLengthOfShortSide { get; set; }

        double MaxLengthOfShortSide { get; set; }

        double MinLengthOfLongSide { get; set; }

        double MaxLengthOfLongSide { get; set; }

        public ISawingTeachingModel<T> MergeTo(ISawingTeachingModel<T> model)
        {
            model.SawOffsetItems = SawOffsetItems;
            model.OutlineWidth = OutlineWidth;
            model.OutlineThreshold = OutlineThreshold;
            model.MinLengthOfShortSide = MinLengthOfShortSide;
            model.MaxLengthOfShortSide = MaxLengthOfShortSide;
            model.MinLengthOfLongSide = MinLengthOfLongSide;
            model.MaxLengthOfLongSide = MaxLengthOfLongSide;
            return model;
        }
    }
}
