namespace GVisionWpf.Models.UiModels
{
    public class WindowLayout
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsFixed { get; set; }

        public WindowLayout() { }

        public WindowLayout(double top, double left, double width, double height, bool isFixed = false)
        {
            Top = top;
            Left = left;
            Width = width;
            Height = height;
            IsFixed = isFixed;
        }
    }
}