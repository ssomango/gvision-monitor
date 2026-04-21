namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface ISinglePackageTeachingModel<T> : IBasePackageModel<T> where T : InspectionTeaching
    {
        Roi PackageRoiTop { get; set; }

        Roi PackageRoiBottom { get; set; }

        Roi PackageRoiLeft { get; set; }

        Roi PackageRoiRight { get; set; }

        public HTuple? ModelHandleForAlign { get; set; }

        public HTuple? HomMat2DModelForAlign { get; set; }

        public Roi PackageModelRoi { get; set; }

        public ISinglePackageTeachingModel<T> MergeTo(ISinglePackageTeachingModel<T> model)
        {
            model.PackageRoiTop.CopyFrom(PackageRoiTop);
            model.PackageRoiBottom.CopyFrom(PackageRoiBottom);
            model.PackageRoiLeft.CopyFrom(PackageRoiLeft);
            model.PackageRoiRight.CopyFrom(PackageRoiRight);

            model.ModelHandleForAlign = ModelHandleForAlign;
            model.HomMat2DModelForAlign = HomMat2DModelForAlign;

            if (PackageModelRoi == null) model.PackageModelRoi = null;
            else model.PackageModelRoi?.CopyFrom(PackageModelRoi);

            model.PackageThreshold = PackageThreshold;
            model.PackageThresholdDiff = PackageThresholdDiff;
            model.PackageEdgeDetectDirection = PackageEdgeDetectDirection;
            model.PackageEdgeDetectMode = PackageEdgeDetectMode;
            return model;
        }
    }
}
