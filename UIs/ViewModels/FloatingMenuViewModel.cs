using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using System.Windows.Input;

namespace GVisionWpf.UIs.ViewModels
{
    public class FloatingMenuViewModel : ViewModelBase
    {
        #region Property

        public ICommand RetryCommand { get; private set; }

        #endregion
        private readonly WindowService _windowService;
        public ICommand OpenChatWindowCommand { get; }

        public FloatingMenuViewModel()
        {
            _windowService = new WindowService();
            OpenChatWindowCommand = new RelayCommand(_windowService.ShowChatWindow);
        }
    }
}
