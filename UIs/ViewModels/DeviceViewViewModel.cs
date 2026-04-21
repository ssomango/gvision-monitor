using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;

namespace GVisionWpf.UIs.ViewModels
{
    public abstract class DeviceViewViewModel : ViewModelBase
    {
        protected readonly List<List<List<RenderableInspectionResult>?>> ResultsInShots; // 검사 결과가 List로 나옵니다. 이건 2차원 배열입니다.
        private ObservableCollection<BlockViewModel> blocks;
        private TableLayout visionTableLayout;
        private TableLayout fovLayout;
        private TableLayout blockLayout;
        private Visibility visibility;

        protected abstract void OpenTeachingWindow(List<InspectionResult>? results);
        protected abstract EColor GetColorOfResult(InspectionResult result);
        public abstract void DisplayResult(List<RenderableInspectionResult>? results);
        public abstract void ClearWindow();

        #region Property

        public ICommand SelectCellCommand { get; }
        public ICommand CellLeftClickCommand { get; }
        public ICommand CellRightClickCommand { get; }

        public ObservableCollection<BlockViewModel> Blocks
        {
            get => this.blocks;
            set => SetField(ref this.blocks, value);
        }

        public TableLayout VisionTableLayout
        {
            get => this.visionTableLayout;
            set
            {
                this.visionTableLayout = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TableRowSize));
                OnPropertyChanged(nameof(TableColSize));
                OnPropertyChanged(nameof(ShotRowSize));
                OnPropertyChanged(nameof(ShotColSize));
                OnPropertyChanged(nameof(BlockRowSize));
                OnPropertyChanged(nameof(BlockColSize));
                InitializeResults();
            }
        }

        public TableLayout FovLayout
        {
            get => this.fovLayout;
            set
            {
                this.fovLayout = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FovRowSize));
                OnPropertyChanged(nameof(FovColSize));
                OnPropertyChanged(nameof(ShotRowSize));
                OnPropertyChanged(nameof(ShotColSize));
                InitializeResults();
            }
        }

        public TableLayout BlockLayout
        {
            get => this.blockLayout;
            set
            {
                this.blockLayout = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BlockRowSize));
                OnPropertyChanged(nameof(BlockColSize));
                InitializeResults();
            }
        }

        public int TableRowSize => this.visionTableLayout.Row;


        public int TableColSize => this.visionTableLayout.Col;

        public int FovRowSize => this.fovLayout.Row == 0 ? 1 : FovLayout.Row;

        public int FovColSize => this.fovLayout.Col == 0 ? 1 : FovLayout.Col;

        public int ShotRowSize
        {
            get
            {
                int rowsPerBlock = VisionTableLayout.Row / BlockRowSize;
                int shotsInBlock = (int)Math.Ceiling((double)rowsPerBlock / (double)FovRowSize);
                return shotsInBlock * BlockRowSize;
            }
        }

        public int ShotColSize
        {
            get
            {
                int colsPerBlock = VisionTableLayout.Col / BlockColSize;
                int shotsInBlock = (int)Math.Ceiling((double)colsPerBlock / (double)FovColSize);
                return shotsInBlock * BlockColSize;
            }
        }

        public int BlockRowSize => this.blockLayout.Row == 0 ? 1 : BlockLayout.Row;

        public int BlockColSize => this.blockLayout.Col == 0 ? 1 : BlockLayout.Col;

        public Thickness CellMargin => new Thickness(1 / Math.Pow(TableRowSize* TableColSize, 0.05));

        public Visibility Visibility
        {
            get => this.visibility;
            set
            {
                this.visibility = value;
                OnPropertyChanged();
            }
        }

        #endregion

        protected DeviceViewViewModel()
        {
            CellLeftClickCommand = new RelayCommand<CellViewModel>(onCellClick);
            SelectCellCommand = new RelayCommand<CellViewModel>(onCellClick);

            CellRightClickCommand = new RelayCommand<CellViewModel>((cell) =>
            {
                if (cell == null)
                {
                    return;
                }

                positionForGrid2PositionForShot(cell.Col, cell.Row, out int xPositionForShot, out int yPositionForShot);

                List<InspectionResult> results = this.ResultsInShots[yPositionForShot][xPositionForShot]
                .Select(renderable => renderable.InspectionResult)
                .ToList();

                OpenTeachingWindow(results);
            });

            this.blocks = new ObservableCollection<BlockViewModel>();
            this.ResultsInShots = new List<List<List<RenderableInspectionResult>?>>();
        }

        // PositionForShot means the coordinate of the image on the grid table.
        public virtual void UpdateResult(List<RenderableInspectionResult> results, int xPositionForShot, int yPositionForShot)
        {
            if (yPositionForShot >= this.ResultsInShots.Count || xPositionForShot >= this.ResultsInShots[yPositionForShot].Count)
            {
                // 범위에 벗어나는 결과를 받은 경우, 업데이트 하지 않고 무시합니다.
                return;
            }

            ReleaseResult(xPositionForShot, yPositionForShot);

            this.ResultsInShots[yPositionForShot][xPositionForShot] = results;
            results.ForEach(r => r.DisposeBy(DisposeBag));

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (RenderableInspectionResult result in results)
                {
                    int xPositionForGrid = result.InspectionResult.XPosition;
                    int yPositionForGrid = result.InspectionResult.YPosition;

                    int blockIdx = CalculateBlockIndex(xPositionForGrid, yPositionForGrid);
                    int cellIdx = CalculateCellIndex(xPositionForGrid, yPositionForGrid);

                    this.blocks[blockIdx].Cells[cellIdx].CellColor = GetColorOfResult(result.InspectionResult);
                }

                OnPropertyChanged(nameof(Blocks));
            });
        }

        private void onCellClick(CellViewModel? clickedCell)
        {
            if (clickedCell == null)
            {
                return;
            }

            TableLayout firstCoordinate = calculateFirstCoordinateOfShot(clickedCell.Col, clickedCell.Row);
            TableLayout lastCoordinate = calculateLastCoordinateOfShot(clickedCell.Col, clickedCell.Row);

            selectCells(firstCoordinate, lastCoordinate);

            positionForGrid2PositionForShot(clickedCell.Col, clickedCell.Row, out int xPositionForShot, out int yPositionForShot);
            DisplayResult(this.ResultsInShots[yPositionForShot][xPositionForShot]);
        }

        private void selectCells(TableLayout firstCoordinateOfSelectedShot, TableLayout lastCoordinateOfSelectedShot)
        {
            // 모든 셀을 순회하지 않고, 이전에 선택된 위치들을 기록해 두는 방식으로 바꾸게 된다면, 코드가 더 복잡해질 수 있습니다.
            // 만약 최적화가 필요하다면 변경하도록 하겠습니다.
            for (int i = 0; i < this.blocks.Count; i++)
            {
                for (int j = 0; j < this.blocks[i].Cells.Count; j++)
                {
                    int row = this.blocks[i].Cells[j].Row;
                    int col = this.blocks[i].Cells[j].Col;
                    if (firstCoordinateOfSelectedShot.Row <= row && row <= lastCoordinateOfSelectedShot.Row &&
                        firstCoordinateOfSelectedShot.Col <= col && col <= lastCoordinateOfSelectedShot.Col)
                    {
                        this.blocks[i].Cells[j].IsSelected = true;
                    }
                    else
                    {
                        this.blocks[i].Cells[j].IsSelected = false;
                    }
                }
            }
        }

        public void ClearResults()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < this.blocks.Count; i++)
                {
                    for (int j = 0; j < this.blocks[i].Cells.Count; j++)
                    {
                        this.blocks[i].Cells[j].IsSelected = false;
                        this.blocks[i].Cells[j].CellColor = EColor.LightGray;
                    }
                }

                OnPropertyChanged(nameof(Blocks));
            });
            ReleaseResults();
        }

        protected void ReleaseResult(int xPositionForShot, int yPositionForShot)
        {
            if (this.ResultsInShots.Count <= yPositionForShot || this.ResultsInShots[yPositionForShot].Count <= xPositionForShot)
            {
                return;
            }

            if (this.ResultsInShots[yPositionForShot][xPositionForShot] == null)
            {
                return;
            }

            foreach (RenderableInspectionResult result in this.ResultsInShots[yPositionForShot][xPositionForShot]!)
            {
                //result.Dispose();
                DisposeAdditionalStuff(result);
            }

            this.ResultsInShots[yPositionForShot][xPositionForShot] = null;
        }

        public void ReleaseResults()
        {
            for (int y = 0; y < this.ResultsInShots.Count; y++)
            {
                for (int x = 0; x < this.ResultsInShots[y].Count; x++)
                {
                    if (this.ResultsInShots[y][x] == null)
                    {
                        return;
                    }

                    ReleaseResult(y, x);
                }
            }
        }

        public virtual void DisposeAdditionalStuff(RenderableInspectionResult result)
        {
            // C# has nice virtual method. So It's intended.
            return;
        }

        protected void InitializeResults()
        {
            ReleaseResults();
            this.ResultsInShots.Clear();
            for (int y = 0; y < ShotRowSize; y++)
            {
                var row = new List<List<RenderableInspectionResult>?>();
                for (int x = 0; x < ShotColSize; x++)
                {
                    row.Add(null);
                }

                this.ResultsInShots.Add(row);
            }

            this.blocks.Clear();
            for (int blockRow = 0; blockRow < BlockRowSize; blockRow++)
            {
                for (int blockCol = 0; blockCol < BlockColSize; blockCol++)
                {
                    int nRowsInBlock = TableRowSize / BlockRowSize;
                    int nColsInBlock = TableColSize / BlockColSize;
                    this.blocks.Add(new BlockViewModel(blockRow, blockCol, nRowsInBlock, nColsInBlock));
                }
            }

            OnPropertyChanged(nameof(Blocks));
        }

        protected int CalculateBlockIndex(int xPositionForGrid, int yPositionForGrid)
        {
            int nRowsInBlock = TableRowSize / BlockRowSize;
            int nColsInBlock = TableColSize / BlockColSize;

            int xPositionForBlock = xPositionForGrid / nColsInBlock;
            int yPositionForBlock = yPositionForGrid / nRowsInBlock;

            return BlockColSize * yPositionForBlock + xPositionForBlock;
        }

        protected int CalculateCellIndex(int xPositionForGrid, int yPositionForGrid)
        {
            int nRowsInBlock = TableRowSize / BlockRowSize;
            int nColsInBlock = TableColSize / BlockColSize;

            int nTotalCols = BlockColSize * nColsInBlock;

            return (yPositionForGrid * nTotalCols + xPositionForGrid) % (nRowsInBlock * nColsInBlock);
        }

        private void positionForGrid2PositionForShot(int xPositionForGrid, int yPositionForGrid, out int xPositionForShot, out int yPositionForShot)
        {
            int nthOfTheBlockX = xPositionForGrid / (TableColSize / BlockColSize) + 1;
            int nthOfTheBlockY = yPositionForGrid / (TableRowSize / BlockRowSize) + 1;
            calculateNMissing(out int nXMissing, out int nYMissing);

            xPositionForShot = (xPositionForGrid + ((nthOfTheBlockX - 1) * nXMissing)) / FovColSize;
            yPositionForShot = (yPositionForGrid + ((nthOfTheBlockY - 1) * nYMissing)) / FovRowSize;
        }

        private void calculateNTotalFov(out int nTotalXFov, out int nTotalYFov)
        {
            nTotalXFov = (int)Math.Ceiling((double)TableColSize / FovColSize);
            nTotalYFov = (int)Math.Ceiling((double)TableRowSize / FovRowSize);
        }

        private void calculateNFovForABlock(out int nXFovForABlock, out int nYFovForABlock)
        {
            calculateNTotalFov(out int nTotalXFov, out int nTotalYFov);

            nXFovForABlock = (int)Math.Ceiling((double)nTotalXFov / BlockColSize);
            nYFovForABlock = (int)Math.Ceiling((double)nTotalYFov / BlockRowSize);
        }

        private void calculateNthOfTheBlock(int xPositionForShot, int yPositionForShot, out int nthOfTheBlockX, out int nthOfTheBlockY)
        {
            calculateNFovForABlock(out int nXFovForABlock, out int nYFovForABlock);
            nthOfTheBlockX = xPositionForShot / nXFovForABlock + 1;
            nthOfTheBlockY = yPositionForShot / nYFovForABlock + 1;
        }

        private TableLayout calculateLastCoordinateOfBlock(int xPositionForGrid, int yPositionForGrid)
        {
            positionForGrid2PositionForShot(xPositionForGrid, yPositionForGrid, out int xPositionForShot, out int yPositionForShot);
            calculateNthOfTheBlock(xPositionForShot, yPositionForShot, out int nthOfTheBlockX, out int nthOfTheBlockY);

            int row = nthOfTheBlockY * (TableRowSize / BlockRowSize) - 1;
            int col = nthOfTheBlockX * (TableColSize / BlockColSize) - 1;

            return new TableLayout(row, col);
        }

        private void calculateNMissing(out int nXMissing, out int nYMissing)
        {
            int nYDevices = TableRowSize / BlockRowSize;
            int nXDevices = TableColSize / BlockColSize;

            nYMissing = (FovRowSize - nYDevices % FovRowSize) % FovRowSize;
            nXMissing = (FovColSize - nXDevices % FovColSize) % FovColSize;
        }

        private TableLayout calculateFirstCoordinateOfShot(int xPositionForGrid, int yPositionForGrid)
        {
            positionForGrid2PositionForShot(xPositionForGrid, yPositionForGrid, out int xPositionForShot, out int yPositionForShot);
            calculateNthOfTheBlock(xPositionForShot, yPositionForShot, out int nthOfTheBlockX, out int nthOfTheBlockY);
            calculateNMissing(out int nXMissing, out int nYMissing);

            int row = FovRowSize * yPositionForShot - ((nthOfTheBlockY - 1) * nYMissing);
            int col = FovColSize * xPositionForShot - ((nthOfTheBlockX - 1) * nXMissing);

            return new TableLayout(row, col);
        }

        private TableLayout calculateLastCoordinateOfShot(int xPositionForGrid, int yPositionForGrid)
        {
            TableLayout firstCoordinate = calculateFirstCoordinateOfShot(xPositionForGrid, yPositionForGrid);
            TableLayout lastCoordinateOfBlock = calculateLastCoordinateOfBlock(xPositionForGrid, yPositionForGrid);

            int lastRow = Math.Min(firstCoordinate.Row + FovRowSize - 1, lastCoordinateOfBlock.Row);
            int lastCol = Math.Min(firstCoordinate.Col + FovColSize - 1, lastCoordinateOfBlock.Col);

            return new TableLayout(lastRow, lastCol);
        }
    }
}