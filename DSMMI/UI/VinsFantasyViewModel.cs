using GVisionWpf.UIs.ViewModels;
using log4net;
using System.Collections.ObjectModel;
using System.Windows;

namespace GVisionWpf.DSMMI.UI
{
    class VinsFantasyViewModel : ViewModelBase
    {
        private static readonly Lazy<VinsFantasyViewModel> lazy = new Lazy<VinsFantasyViewModel>(() => new VinsFantasyViewModel());
        public static VinsFantasyViewModel Instance => lazy.Value;

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        public event EventHandler<object>? ScrollToNewItemRequested;
        private static readonly ILog log = LogManager.GetLogger("DSMMI");

        private int mapDelay = 300, prsDelay = 1000;

        public int MapDelay
        {
            get => this.mapDelay;
            set => SetField(ref this.mapDelay, value);
        }

        public int PrsDelay
        {
            get => this.prsDelay;
            set => SetField(ref this.prsDelay, value);
        }

        private VinsFantasyViewModel() { }

        public void Print(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string item = $"[{DateTime.Now:MM'/'dd HH:mm:ss}] {message}";
                Logs.Add(item);
                ScrollToNewItemRequested?.Invoke(this, item);
            });
            log.Info(message);
        }
    }
}
