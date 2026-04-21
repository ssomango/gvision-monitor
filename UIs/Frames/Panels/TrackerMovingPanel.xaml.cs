using GVisionWpf.Models.UiModels;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// TrackerMovingPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TrackerMovingPanel : UserControl
    {
        /*
         * MEMO:
         * Tracker 이슈 이외에 모든 작업 아마도 테스트완료됨. 라스트 댄스 이전 (07/27)
         * 트래커 이슈 있는데, 이거 Roi 스노우볼의 영향을 받음. 작업이 필요함. 트래커 이슈는 아닌걸로 일단 파악중
         */

        public int MovingPixelSize { get; set; } = 1;

        public TrackerMovingPanel()
        {
            InitializeComponent();
            initTrackerButtons();
        }

        private void initTrackerButtons()
        {
            List<(string, RoiMove)> buttonStructs = buttonStructsBuilder();
            foreach ((string, RoiMove) buttonStruct in buttonStructs)
            {
                if (FindName(buttonStruct.Item1) is Button button)
                {
                    button.Tag = buttonStruct.Item2;
                    button.Click += trackerButtonClicked;
                }
            }
        }

        private List<(string, RoiMove)> buttonStructsBuilder()
        {
            return new List<(string, RoiMove)>
            {
                ("xLeftPlusButton", new RoiMove(-MovingPixelSize, 0, 0, 0)),
                ("xBottomLeftPlusButton", new RoiMove(-MovingPixelSize, 0, 0, MovingPixelSize)),
                ("xTopPlusButton", new RoiMove(0, -MovingPixelSize, 0, 0)),
                ("xBottomPlusButton", new RoiMove(0, 0, 0, MovingPixelSize)),
                ("xTopRightPlusButton", new RoiMove(0, -MovingPixelSize, MovingPixelSize, 0)),
                ("xTopLeftPlusButton", new RoiMove(-MovingPixelSize, -MovingPixelSize, 0, 0)),
                ("xRightPlusButton", new RoiMove(0, 0, MovingPixelSize, 0)),
                ("xBottomRightPlusButton", new RoiMove(0, 0, MovingPixelSize, MovingPixelSize)),
                ("xTopLeftMinusButton", new RoiMove(MovingPixelSize, MovingPixelSize, 0, 0)),
                ("xTopMinusButton", new RoiMove(0, MovingPixelSize, 0, 0)),
                ("xTopRightMinusButton", new RoiMove(0, MovingPixelSize, -MovingPixelSize, 0)),
                ("xRightMinusButton", new RoiMove(0, 0, -MovingPixelSize, 0)),
                ("xBottomRightMinusButton", new RoiMove(0, 0, -MovingPixelSize, -MovingPixelSize)),
                ("xBottomMinusButton", new RoiMove(0, 0, 0, -MovingPixelSize)),
                ("xBottomLeftMinusButton", new RoiMove(MovingPixelSize, 0, 0, -MovingPixelSize)),
                ("xLeftMinusButton", new RoiMove(MovingPixelSize, 0, 0, 0))
            };
        }

        private static int[] convertAdjustToMove(int[] roiMoveArray)
        {
            int topLeftXMove = roiMoveArray[0];
            int topLeftYMove = roiMoveArray[1];
            int bottomRightXMove = roiMoveArray[2];
            int bottomRightYMove = roiMoveArray[3];

            if (topLeftXMove != 0)
                bottomRightXMove = topLeftXMove;
            if (topLeftYMove != 0)
                bottomRightYMove = topLeftYMove;

            if (bottomRightXMove != 0)
                topLeftXMove = bottomRightXMove;
            if (bottomRightYMove != 0)
                topLeftYMove = bottomRightYMove;

            return new int[] { topLeftXMove, topLeftYMove, bottomRightXMove, bottomRightYMove };
        }

        private void trackerButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            RoiMove roiRoiMove = (RoiMove)button.Tag;
            trackerMovingLogic(roiRoiMove);
        }

        private void trackerMovingLogic(RoiMove roiMove)
        {
            HTuple? drawingObjectId = GlobalTrackerTarget.Instance.SelectedDrawingObjectId;

            if (drawingObjectId == null)
            {
                return;
            }

            HTuple paramNames = new HTuple("column1", "row1", "column2", "row2");
            HOperatorSet.GetDrawingObjectParams(drawingObjectId, paramNames, out HTuple curPosition);

            int[] roiMoveArray = roiMove.ToArray();
            if ((bool)this.xRoiAdjustToRoiMoveToggleCheckBox.IsChecked!)
            {
                roiMoveArray = convertAdjustToMove(roiMoveArray);
            }

            int[] newPosition =
            {
                Convert.ToInt32(curPosition.DArr[0]) + roiMoveArray[0],
                Convert.ToInt32(curPosition.DArr[1]) + roiMoveArray[1],
                Convert.ToInt32(curPosition.DArr[2]) + roiMoveArray[2],
                Convert.ToInt32(curPosition.DArr[3]) + roiMoveArray[3],
            };

            HOperatorSet.SetDrawingObjectParams(drawingObjectId, paramNames, newPosition);
            GlobalTrackerTarget.Instance.InvokeCallBack();
        }

        private void disableMinusButton_Checked(object sender, RoutedEventArgs e)
        {
            setButtonsEnabled(this.xMinusButtonGrid, false);
        }

        private void enableMinusButton_Unchecked(object sender, RoutedEventArgs e)
        {
            setButtonsEnabled(this.xMinusButtonGrid, true);
        }

        private static void setButtonsEnabled(Grid container, bool isEnabled)
        {
            foreach (Button child in container.Children)
            {
                child.IsEnabled = isEnabled;
            }
        }
    }
}