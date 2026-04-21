using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// AlertWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlertWindow : Window
    {
        public enum EAlert { YESNO = 0, YES = 1, TEXT = 2, IMAGE = 3 }
        public enum EIcon { ALERT, CHECK }

        private BitmapImage ALERT_IMAGE = new BitmapImage(new Uri("/Assets/Icons/alert.png", UriKind.Relative));
        private BitmapImage CHECK_IMAGE = new BitmapImage(new Uri("/Assets/Icons/check.png", UriKind.Relative));

        private readonly List<DeviceViewWindow>? deviceViewWindows;

        public AlertWindow()
        {
            InitializeComponent();

            this.xTitle.Content = "Alert";
            setIcon(EIcon.ALERT);
            this.xMessage.Text = "Are you sure?";
        }

        #region Custom

        public AlertWindow(string title, string message, EAlert alert, List<DeviceViewWindow> deviceViewWindows)
        {
            InitializeComponent();

            selectMode(alert);

            this.xTitle.Content = title;
            this.xMessage.Text = message;
            this.deviceViewWindows = deviceViewWindows;
        }

        public AlertWindow(string title, string message, EAlert alert)
        {
            InitializeComponent();

            selectMode(alert);

            this.xTitle.Content = title;
            this.xMessage.Text = message;
        }

        public AlertWindow(string title, string message, EAlert alert, TimeSpan timeout)
        {
            InitializeComponent();

            selectMode(alert);

            this.xTitle.Content = title;
            this.xMessage.Text = message;

            int count = 0;
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                if (this.DialogResult == true)
                {
                    timer.Stop();
                    return;
                }

                count++;

                this.xOkButton.Content = $"OK ({timeout.TotalSeconds - count})";

                if (count >= timeout.TotalSeconds)
                {
                    this.DialogResult = true;
                }
            };

            timer.Start();
        }

        public AlertWindow(string title, EIcon icon, string message)
        {
            InitializeComponent();

            this.xTitle.Content = title;
            setIcon(icon);
            this.xMessage.Text = message;
        }

        public AlertWindow(string title, EIcon icon, string message, EAlert alert)
        {
            InitializeComponent();

            selectMode(alert);
            setIcon(icon);

            this.xTitle.Content = title;
            this.xMessage.Text = message;
        }

        public AlertWindow(string title, string message, EAlert alert, string imagePath)
        {
            InitializeComponent();

            selectMode(alert);

            this.xTitle.Content = title;
            this.xMessage.Text = message;
            this.xImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
        }

        public AlertWindow(string title, EIcon icon, string message, EAlert alert, string imagePath)
        {
            InitializeComponent();

            selectMode(alert);
            setIcon(icon);

            this.xTitle.Content = title;
            this.xMessage.Text = message;
            this.xImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
        }

        #endregion

        private void selectMode(EAlert alert)
        {
            switch (alert)
            {
                case EAlert.YESNO:
                    this.xIcon.Source = this.ALERT_IMAGE;
                    break;

                case EAlert.YES:
                    this.xIcon.Source = this.CHECK_IMAGE;
                    this.xButtonStackPanel.Visibility = Visibility.Collapsed;
                    this.xOkButton.Content = "OK";
                    break;

                case EAlert.TEXT:
                    this.xIconStackPanel.Visibility = Visibility.Collapsed;
                    this.xButtonStackPanel.Visibility = Visibility.Collapsed;
                    this.xOkButton.Content = "OK";
                    this.xMessage.FontSize = 13;
                    this.xMessage.Height = 250;

                    adjustTextBoxWidth(this.xMessage);
                    Width = this.xMessage.Width;
                    break;

                case EAlert.IMAGE:
                    this.Width = 700;
                    this.Height = 600;
                    this.xButtonStackPanel.Visibility = Visibility.Collapsed;
                    this.xImageModeStackPanel.Visibility = Visibility.Visible;
                    this.xOkButton.Content = "OK";
                    this.xIcon.Width = 60;
                    break;

                default:
                    this.xIcon.Source = this.ALERT_IMAGE;
                    break;
            }
        }

        private void setIcon(EIcon icon)
        {
            switch (icon)
            {
                case EIcon.ALERT:
                    this.xIcon.Source = ALERT_IMAGE;
                    break;
                case EIcon.CHECK:
                    this.xIcon.Source = CHECK_IMAGE;
                    break;
                default:
                    this.xIcon.Source = ALERT_IMAGE;
                    break;
            }
        }

        private void ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void adjustTextBoxWidth(TextBox textBox)
        {
            var formattedText = new FormattedText(
                textBox.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                Brushes.Transparent,
                new NumberSubstitution(),
                1);

            textBox.Width = formattedText.Width + 400;
        }

        private void previewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
