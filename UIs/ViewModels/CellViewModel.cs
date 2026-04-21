namespace GVisionWpf.UIs.ViewModels
{
    public class CellViewModel : ViewModelBase
    {
        private EColor cellColor = EColor.LightGray;
        private bool isSelected;

        public int Row { get; }
        public int Col { get; }

        public bool IsSelected
        {
            get => this.isSelected;
            set => SetField(ref this.isSelected, value);
        }

        public EColor CellColor
        {
            get => this.cellColor;
            set => SetField(ref this.cellColor, value);
        }

        public CellViewModel(int row, int col)
        {
            Row = row;
            Col = col;
            IsSelected = false;
        }
    }
}