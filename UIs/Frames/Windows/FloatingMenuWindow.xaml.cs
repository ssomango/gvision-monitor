using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// FloatingMenuWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FloatingMenuWindow : FloatingWindowBase
    {
        private bool isFloatingMenuOpen;

        private bool isDragging = false;
        private Stopwatch mouseDownTimer;
        private readonly double dragThreshold = 50;

        public FloatingMenuWindow() : base("FloatingMenu")
        {
            InitializeComponent();

            this.isFloatingMenuOpen = true;
            this.xFloatingMenuStackPanel.Visibility = Visibility.Visible;
            setPosition();

            this.mouseDownTimer = new Stopwatch();
        }

        private void setPosition()
        {
            this.Left = 1280 - 70;
            //this.Left = 0;
            this.Top = 864 - 155;
        }

        private void floatingButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.isDragging = false;

            this.mouseDownTimer.Restart();

            (sender as UIElement)?.CaptureMouse();
        }

        private void floatingButton_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.mouseDownTimer.ElapsedMilliseconds > this.dragThreshold)
                {
                    this.isDragging = true;
                    this.DragMove();
                }
            }
        }

        private void floatingButton_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.isDragging)
            {
                if (this.isFloatingMenuOpen)
                {
                    this.xFloatingMenuStackPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.xFloatingMenuStackPanel.Visibility = Visibility.Visible;
                }

                this.isFloatingMenuOpen = !this.isFloatingMenuOpen;
            }

            (sender as UIElement)?.ReleaseMouseCapture();

            this.mouseDownTimer.Stop();

        }
    }
}
