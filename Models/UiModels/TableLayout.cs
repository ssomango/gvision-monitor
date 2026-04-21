namespace GVisionWpf.Models.UiModels
{
    public struct TableLayout
    {
        public int Row;
        public int Col;

        public TableLayout(int row, int col)
        {
            this.Row = row;
            this.Col = col;
        }
    }
}
