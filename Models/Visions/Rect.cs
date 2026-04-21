namespace GVisionWpf.Models.Visions
{
    public struct Rect
    {
        public double Row1 { get; set; }
        public double Col1 { get; set; }
        public double Row2 { get; set; }
        public double Col2 { get; set; }

        public Rect()
        {
            Row1 = 500;
            Col1 = 500;
            Row2 = 1000;
            Col2 = 1000;
        }

        public Rect(double row1, double col1, double row2, double col2)
        {
            Row1 = row1;
            Col1 = col1;
            Row2 = row2;
            Col2 = col2;
        }

        public Rect(Roi roi)
        {
            Row1 = roi.Row1;
            Col1 = roi.Col1;
            Row2 = roi.Row2;
            Col2 = roi.Col2;
        }
    }
}
