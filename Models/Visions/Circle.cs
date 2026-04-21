namespace GVisionWpf.Models.Visions
{
    public struct Circle
    {
        public double X;
        public double Y;
        public double Radius;

        public Circle(double x, double y, double radius)
        {
            this.X = x;
            this.Y = y;
            this.Radius = radius;
        }
    }
}
