
namespace GVisionWpf.Models.Entities.Result
{
    using GVisionWpf.Visions;
    using InspectionResultDrawings = System.Collections.Generic.List<(HalconDotNet.HObject drawingObject, GVisionWpf.Types.EColor color)>;

    public class InspectionRenderData : IDisposable
    {

        #region Fields
        private List<FixedText> fixedTexts = new List<FixedText>();

        private List<FloatingText> floatingTexts = new List<FloatingText>();

        private HTuple? matrix;
        #endregion

        #region Properties
        public InspectionResultDrawings ResultDrawings = new InspectionResultDrawings();

        public List<FloatingText> FloatingTexts => floatingTexts;

        public List<FixedText> FixedTexts => fixedTexts.OrderBy(x => x.Sequence).ToList();

        #endregion

        public void Dispose()
        {
            ResultDrawings.ForEach(r => r.drawingObject.Dispose());
            matrix?.Dispose();
        }

        public void MergeWith(InspectionRenderData other)
        {
            ResultDrawings.AddRange(other.ResultDrawings);
            fixedTexts.AddRange(other.FixedTexts);
            floatingTexts.AddRange(other.FloatingTexts);

            matrix = other.matrix;
        }

        public void AddMatrix(HTuple matrix) => this.matrix = matrix;

        public void AddText(FixedText text) => this.fixedTexts.Add(text);

        public void AddText(FloatingText text) => this.floatingTexts.Add(text);

        public void ClearTexts()
        {
            fixedTexts.Clear();
        }
    }

    public class RenderableInspectionResult : IDisposable
    {
        public InspectionResult InspectionResult;

        public InspectionRenderData RenderData;
   

        public RenderableInspectionResult(InspectionResult inspectionResult, InspectionRenderData renderData)
        {
            this.InspectionResult = inspectionResult;
            this.RenderData = renderData;
        }

        public void Dispose()
        {
            InspectionResult.Dispose();
            RenderData.Dispose();
        }

        public override string ToString()
        {
            return InspectionResult.ToString();
        }
    }
}
