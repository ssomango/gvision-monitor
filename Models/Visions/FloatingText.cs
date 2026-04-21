namespace GVisionWpf.Models.Visions
{
    public class FloatingText : Text
    {
        public Point Point { get; set; }

        public FloatingText(string content, Point point, EColor color = EColor.White, int fontSize = 14)
        {
            Content = content;
            Point = point;
            Color = color;
            FontSize = fontSize;
        }

        public FloatingText(FixedText text)
        {
            Content = text.Content;
            Point = new Point(5, 5);
            Color = text.Color;
            FontSize = text.FontSize;
        }
    }
}