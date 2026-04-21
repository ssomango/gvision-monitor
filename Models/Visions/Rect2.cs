namespace GVisionWpf.Models.Visions
{
    public struct Rect2
    {
        public double Row { get; set; }
        public double Col { get; set; }
        public double Phi { get; set; }
        public double Length1 { get; set; }
        public double Length2 { get; set; }

        public Rect2()
        {
            Row = 500;
            Col = 500;
            Phi = 1000;
            Length1 = 1000;
            Length2 = 1000;
        }

        public Rect2(double row, double col, double phi, double length1, double length2)
        {
            Row = row;
            Col = col;
            Phi = phi;
            Length1 = length1;
            Length2 = length2;
        }
    }
}
