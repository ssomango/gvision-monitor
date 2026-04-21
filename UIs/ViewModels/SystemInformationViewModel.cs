using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class SystemInformationViewModel : ViewModelBase
    {
        private readonly ObservableCollection<string> logs = new ObservableCollection<string>();
        private static readonly Lazy<SystemInformationViewModel> lazy = new Lazy<SystemInformationViewModel>(() => new SystemInformationViewModel());
        public static SystemInformationViewModel Instance => lazy.Value;
        private bool hasNewItems;
        private const int MAX_LOG_COUNT = 10000;

        public SystemInformationViewModel() 
        {
            GVisionMessenger.Instance.Register(this);
        }

        #region Property

        public bool HasNewItems
        {
            get => this.hasNewItems;
            set => SetField(ref this.hasNewItems, value);
        }

        public ObservableCollection<string> Logs => this.logs;

        #endregion

        public void Print(string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (Logs.Count > MAX_LOG_COUNT * 2)
                {
                    removeOldLogs();
                }

                string item = $"[{DateTime.Now:MM'/'dd HH:mm:ss}] {message}";
                Logs.Add(item);

                HasNewItems = true;
            });
        }

        private void removeOldLogs()
        {
            int itemsToRemove = Logs.Count - MAX_LOG_COUNT + 1;
            for (int i = 0; i < itemsToRemove; i++)
            {
                Logs.RemoveAt(0);
            }
        }
    }

    partial class SystemInformationViewModel : IRecipient<string>
    {
        public void Receive(string message)
        {
            Print(message);
        }
    }
}