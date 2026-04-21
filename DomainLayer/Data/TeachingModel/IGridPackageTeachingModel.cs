namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IGridPackageTeachingModel<T> : IBasePackageModel<T> where T : InspectionTeaching
    {

        public int SelectedPackageIndex { get; set; }

        public int RowSize { get; set; }

        public int ColumnSize { get; set; }

        public double RotateAngle { get; set; }

        public Roi PackageRoi { get; set; }

        public Dictionary<int, int> ShotNoByTabNo { get; set; }

        public IGridPackageTeachingModel<T> MergeTo(IGridPackageTeachingModel<T> model)
        {
            model.PackageRoi.CopyFrom(PackageRoi);

            model.RowSize = RowSize;
            model.ColumnSize = ColumnSize;
            model.PackageThresholdDiff = PackageThresholdDiff;
            model.PackageEdgeDetectDirection = PackageEdgeDetectDirection;
            model.PackageEdgeDetectMode = PackageEdgeDetectMode;
            return model;
        }
    }
}
