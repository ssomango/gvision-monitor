using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class ResultViewStackViewModel : ViewModelBase
    {
        [ObservableProperty]
        private EResultType errorType;

        [ObservableProperty]
        private int count = 0;

        [ObservableProperty]
        private Brush color = Brushes.Black;

        [ObservableProperty]
        private string standardValue = string.Empty;

        [ObservableProperty]
        private Visibility standardValueVisibility = Visibility.Collapsed;


        public ResultViewStackViewModel(EResultType errorType, Brush color, string standardValue = "", Visibility standardValueVisibility = Visibility.Collapsed)
        {
            ErrorType = errorType;
            Color = color;
            StandardValue = standardValue;
            StandardValueVisibility = standardValueVisibility;
        }
    }
}
