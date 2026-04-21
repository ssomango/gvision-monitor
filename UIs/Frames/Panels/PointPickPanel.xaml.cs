using GVisionWpf.GlobalStates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Point = GVisionWpf.Models.Visions.Point;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// PointPickPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PointPickPanel : UserControl
    {
        public event EventHandler? PointSelected;

        public static readonly DependencyProperty PointProperty = DependencyProperty.Register(nameof(Point), typeof(Point), typeof(PointPickPanel), new PropertyMetadata(new Point(0, 0)));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(PointPickPanel), new PropertyMetadata("Point"));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(string), typeof(PointPickPanel), new PropertyMetadata("green"));

        #region Property

        public Point Point
        {
            get => (Point)GetValue(PointProperty);
            set => SetValue(PointProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Color
        {
            get => (string)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        #endregion

        public PointPickPanel()
        {
            InitializeComponent();
        }

        private async void pickBtn_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CurrentTeachingWindow.Instance.Window == null)
            {
                throw new NullReferenceException("티칭창에서 VisionWindow를 등록하지 않았습니다.");
            }

            await Task.Run(() =>
            {
                CurrentTeachingWindow.Instance.Window.xHSmartWindow.HalconWindow.DrawPoint(out double row, out double column);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Point.Row = row;
                    Point.Col = column;

                    PointSelected?.Invoke(this, new ItemEventArgs<Point>(Point));
                });
            });
        }
    }
}