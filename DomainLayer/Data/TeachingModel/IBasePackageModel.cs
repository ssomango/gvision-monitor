namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IBasePackageModel<T> where T : InspectionTeaching
    {
        public Pose PackageCenter { get; set; }

        public Threshold PackageThreshold { get; set; }

        public int PackageThresholdDiff { get; set; }

        public EEdgeDetectDirection PackageEdgeDetectDirection { get; set; }

        public EEdgeDetectMode PackageEdgeDetectMode { get; set; }
    }
}
