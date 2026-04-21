using GVisionWpf.Api;
using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// HistoryWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();

            if (DataContext is HistoryViewModel viewModel)
            {
                viewModel.SmartWindow = this.xHSmartWindow;
            }
        }
        // value 값 조정을 위해 추가_AML 에서 추가...
        public HistoryViewModel ViewModel => this.DataContext as HistoryViewModel;


        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double threshold = 30.0;

            if (e.VerticalOffset >= e.ExtentHeight - e.ViewportHeight - threshold)
            {
                var viewModel = DataContext as HistoryViewModel;
                if (viewModel != null && viewModel.LoadMoreDataCommand.CanExecute(null))
                {
                    viewModel.LoadMoreDataCommand.Execute(null);
                }
            }
        }

        private void ASButton_Click(object sender, RoutedEventArgs e)
        {
            new ASWindow().ShowDialog();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
