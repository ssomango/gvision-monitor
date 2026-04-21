namespace GVisionWpf.Models.Visions
{
    public class FixedText : Text
    {
        public double Sequence { get; set; }

        public FixedText(string content, double sequence, EColor color = EColor.White, int fontSize = 16)
        {
            Content = content;
            Sequence = sequence;
            Color = color;
            FontSize = fontSize;
        }
    }
}