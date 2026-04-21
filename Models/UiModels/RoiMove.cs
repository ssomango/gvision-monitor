namespace GVisionWpf.Models.UiModels
{
    public class RoiMove
    {
        public int Col1Move { get; set; }
        public int Row1Move { get; set; }
        public int Col2Move { get; set; }
        public int Row2Move { get; set; }

        public RoiMove(int colum1, int row1, int column2, int row2)
        {
            Col1Move = colum1;
            Row1Move = row1;
            Col2Move = column2;
            Row2Move = row2;
        }

        public int[] ToArray()
        {
            return new int[] { Col1Move, Row1Move, Col2Move, Row2Move, };
        }
    }
}
