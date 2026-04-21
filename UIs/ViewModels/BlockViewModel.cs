using System.Collections.ObjectModel;

namespace GVisionWpf.UIs.ViewModels
{
    public class BlockViewModel : ViewModelBase
    {
        public ObservableCollection<CellViewModel> Cells { get; set; }

        public int BlockRow { get; }
        public int BlockCol { get; }
        public int CellRowSize { get; }
        public int CellColSize { get; }

        public BlockViewModel(int blockRow, int blockCol, int cellRowSize, int cellColSize)
        {
            BlockRow = blockRow;
            BlockCol = blockCol;
            CellRowSize = cellRowSize;
            CellColSize = cellColSize;

            Cells = new ObservableCollection<CellViewModel>();

            for (int row = 0; row < CellRowSize; row++)
            {
                for (int col = 0; col < CellColSize; col++)
                {
                    int xPositionForGrid = blockCol * CellColSize + col;
                    int yPositionForGrid = blockRow * CellRowSize + row;
                    Cells.Add(new CellViewModel(yPositionForGrid, xPositionForGrid));
                }
            }
        }
    }
}