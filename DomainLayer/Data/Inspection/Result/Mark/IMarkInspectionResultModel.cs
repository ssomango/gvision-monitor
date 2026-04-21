using GVisionWpf.Models.Entities.Result;

namespace GVisionWpf.DomainLayer.Data.Inspection.Result.Mark
{
    public interface IMarkInspectionResultModel<T> : IInspectionResultModel where T : InspectionResult
    {
        public Result<int> MissingCharacter { get; set; }

        public Result<int> NoMark { get; set; }

        public Result<string> Mark { get; set; }

        public Result<Pose> TextOffset { get; set; }

        public IMarkInspectionResultModel<MapInspectionResult> MergeTo(IMarkInspectionResultModel<MapInspectionResult> model)
        {
            model.MissingCharacter = MissingCharacter;
            model.NoMark = NoMark;
            model.Mark = Mark;
            model.TextOffset = TextOffset;
            return model;
        }
    }
}
