namespace GVisionWpf.Models.Entities.Result
{
    public class SkipInspectionResult : InspectionResult
    {
        public SkipInspectionResult()
        {
            this.HasDevice = new Result<bool>(EResultType.NoDevice, false);
            this.PackageOffset = new Result<Pose>(EResultType.Good, new Pose(0, 0, 0));
        }


        public override EResultType ErrorType()
        {
            return EResultType.NotInUsePicker;
        }
    }
}