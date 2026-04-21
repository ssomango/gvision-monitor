using GVisionWpf.Models.UiModels;

namespace GVisionWpf.Models.Visions
{
    public class MarkItem
    {
        public EMarkMode Mode;
        public Roi Roi;

        public HTuple? ShapeMatchingModel;
        public int MinMatchingRate;
        public int nCharacters;
        public string OcrText;
        public HObject connectedTextRegion;

        public MarkItem()
        {
            this.Mode = EMarkMode.Ocr;
            this.Roi = new Roi("MARK");
            this.MinMatchingRate = 0;
            this.nCharacters = 0;
            this.OcrText = string.Empty;
        }

        public MarkItem(MarkItemSource markItemSource)
        {
            this.Mode = markItemSource.IsOcrMode ? EMarkMode.Ocr : EMarkMode.ShapeMatching;
            this.Roi = markItemSource.Roi;
            this.MinMatchingRate = markItemSource.MinMatchingRate;
            this.nCharacters = markItemSource.nCharacters;
            this.OcrText = markItemSource.OcrText;
        }
    }
}