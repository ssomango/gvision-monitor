using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GVisionWpf.UIs.Frames.Windows
{
    // mainWindowOnLocationChanged 이벤트로 IsMaximized 변경이 전파 되지 않아 임시처리
    public class FloatingWindowBase : Window, INotifyPropertyChanged
    {
        private bool isFixed;
        private bool isMaximized;
        private bool canMaximized;
        private string windowTitle = string.Empty;
        private readonly WindowLayoutRepository repository = WindowLayoutRepository.Instance;

        public event PropertyChangedEventHandler? PropertyChanged;

        #region Property

        protected string WindowTitle
        {
            get => this.windowTitle;
            set
            {
                this.windowTitle = value;
                Title = this.windowTitle;
            }
        }

        public bool IsFixed
        {
            get => this.isFixed;
            set
            {
                this.isFixed = value;
            }
        }

        public bool IsMaximized
        {
            get => this.isMaximized;
            set
            {
                this.isMaximized = value;
                OnPropertyChanged();
            }
        }

        public bool CanMaximized
        {
            get => this.canMaximized;
            set => this.canMaximized = value;
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FloatingWindowBase(string windowTitle, bool canMaximized = false)
        {
            WindowTitle = windowTitle;
            CanMaximized = canMaximized;
            IsMaximized = false;

            loadWindowLayout();
            Closing += onClosing;
            IsVisibleChanged += onIsVisibleChanged;

            Window mainWindow = Application.Current.MainWindow!;
            mainWindow.LocationChanged += mainWindowOnLocationChanged;
            mainWindow.Closing += mainWindowOnClosing;
            Owner = mainWindow;

            ShowInTaskbar = false;
        }

        private void loadWindowLayout()
        {
            WindowLayout relativeLayout = this.repository.GetLayout(this.windowTitle);

            Window mainWindow = Application.Current.MainWindow!;
            Top = mainWindow.Top + relativeLayout.Top;
            Left = mainWindow.Left + relativeLayout.Left;
            Width = relativeLayout.Width;
            Height = relativeLayout.Height;
            IsFixed = relativeLayout.IsFixed;

            if (Left < 0)
            {
                Left = 0;
            }

            if (Top < 0)
            {
                Top = 0;
            }
        }

        private void saveWindowLayout()
        {
            Window mainWindow = Application.Current.MainWindow!;

            WindowLayout relativeLayout = new WindowLayout
            {
                Top = Top - mainWindow.Top,
                Left = Left - mainWindow.Left,
                Width = Width,
                Height = Height,
                IsFixed = IsFixed,
            };

            this.repository.SaveLayout(this.windowTitle, relativeLayout);
        }

        private void restoreAndSaveWindowState()
        {
            if (IsMaximized)
            {
                loadWindowLayout();
                IsMaximized = false;
            }

            saveWindowLayout();
        }

        private void onClosing(object? sender, CancelEventArgs e)
        {
            restoreAndSaveWindowState();
        }

        private void onIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 윈도우를 닫는 것에 대신 Visibility를 변경하는 방식을 사용중
            if (IsVisible)
            {
                return;
            }

            restoreAndSaveWindowState();
        }

        private void mainWindowOnLocationChanged(object? sender, EventArgs e)
        {
            if (IsMaximized)
            {
                IsMaximized = false;
            }

            loadWindowLayout();
        }

        private void mainWindowOnClosing(object? sender, CancelEventArgs e)
        {
            restoreAndSaveWindowState();
        }
    }
}