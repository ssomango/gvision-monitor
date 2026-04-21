namespace GVisionWpf.Models.Visions
{
    public class Text
    {
        public string Content { get; set; } = string.Empty;
        public EColor Color { get; set; }
        public int FontSize { get; set; }

        public string Font
        {
            get => $"default-{FontSize}";
        }
    }
}