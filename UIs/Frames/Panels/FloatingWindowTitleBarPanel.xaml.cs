using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.Overlays;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// FloatingWindowTitleBarPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FloatingWindowTitleBarPanel : UserControl
    {
        private readonly WindowLayoutRepository repository = WindowLayoutRepository.Instance;

        #region Property

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(FloatingWindowTitleBarPanel),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty CanMaximizedProperty =
            DependencyProperty.Register(
                nameof(CanMaximized),
                typeof(bool),
                typeof(FloatingWindowTitleBarPanel),
                new PropertyMetadata(false));

        public bool CanMaximized
        {
            get { return (bool)GetValue(CanMaximizedProperty); }
            set { SetValue(CanMaximizedProperty, value); }
        }

        public static readonly DependencyProperty IsFixedProperty =
            DependencyProperty.Register(
                nameof(IsFixed),
                typeof(bool),
                typeof(FloatingWindowTitleBarPanel),
                new PropertyMetadata(false));

        public bool IsFixed
        {
            get { return (bool)GetValue(IsFixedProperty); }
            set { SetValue(IsFixedProperty, value); }
        }

        public static readonly DependencyProperty IsMaximizedProperty =
            DependencyProperty.Register(
                nameof(IsMaximized),
                typeof(bool),
                typeof(FloatingWindowTitleBarPanel),
                new PropertyMetadata(false));

        public bool IsMaximized
        {
            get { return (bool)GetValue(IsMaximizedProperty); }
            set { SetValue(IsMaximizedProperty, value); }
        }

        #endregion

        public FloatingWindowTitleBarPanel()
        {
            InitializeComponent();
        }

        #region Event

        private void onPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsFixed || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Window? parentWindow = Window.GetWindow(this);
            if (parentWindow == null)
            {
                return;
            }

            parentWindow.DragMove();

            if (parentWindow.Left < 0)
            {
                parentWindow.Left = 0;
            }

            if (parentWindow.Top < 0)
            {
                parentWindow.Top = 0;
            }
        }

        private void fixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsFixed = true;
            changeResizeMode(ResizeMode.NoResize);

            if (!IsMaximized)
            {
                saveWindowLayout();
            }
        }

        private void fixCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            IsFixed = false;
            changeResizeMode(ResizeMode.CanResize);
        }

        private void toggleLayoutButtonClick(object sender, RoutedEventArgs e)
        {
            if (!CanMaximized)
            {
                return;
            }

            if (!IsMaximized)
            {
                maximizeLayout();
            }

            else
            {
                initWindowLayout();
            }

            IsMaximized = !IsMaximized;
        }

        #endregion

        private void changeResizeMode(ResizeMode resizeMode)
        {
            Window? parentWindow = Window.GetWindow(this);

            if (parentWindow != null)
            {
                parentWindow.ResizeMode = resizeMode;
            }
        }

        // FloatingWindowBase에 쉽게 접근 하는 방법을 모르겠어서 일단 이렇게 처리
        private void saveWindowLayout()
        {
            Window mainWindow = Application.Current.MainWindow!;
            Window? parentWindow = Window.GetWindow(this);

            if (parentWindow == null)
            {
                return;
            }

            WindowLayout currentLayout = new WindowLayout
            {
                Top = parentWindow.Top - mainWindow.Top,
                Left = parentWindow.Left - mainWindow.Left,
                Width = parentWindow.Width,
                Height = parentWindow.Height,
                IsFixed = IsFixed,
            };

            this.repository.SaveLayout(Title, currentLayout);
        }

        private void maximizeLayout()
        {
            saveWindowLayout();

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                var targetElement = mainWindow.ResolveAnchorElement("MAIN_MAINFRAME") as FrameworkElement;
                if (targetElement == null)
                {
                    return;
                }

                Point relativePoint = targetElement.TransformToAncestor(mainWindow).Transform(new Point(0, 0));

                double top = relativePoint.Y;
                double left = relativePoint.X;
                double width = targetElement.ActualWidth;
                double height = targetElement.ActualHeight;

                // TODO: MainWindow 최상단의 모서리가 둥근 회색 영역의 높이 값의 크기 7px 만큼의 미묘한 좌표 값 만큼의 차이가 생기는데 해결방법을 못 찾아서 임시처리
                top += 7;
                left += 7;

                WindowLayout maximizeLayout = new WindowLayout(top, left, width, height);
                setWindowLayout(maximizeLayout);
            }
        }

        private void initWindowLayout()
        {
            WindowLayout restoreLayout = this.repository.GetLayout(Title);
            setWindowLayout(restoreLayout);
        }

        private void setWindowLayout(WindowLayout windowLayout)
        {
            Window? window = Window.GetWindow(this);

            if (window != null)
            {
                Window mainWindow = Application.Current.MainWindow!;
                window.Top = mainWindow.Top + windowLayout.Top;
                window.Left = mainWindow.Left + windowLayout.Left;
                window.Width = windowLayout.Width;
                window.Height = windowLayout.Height;
            }
        }
    }
}