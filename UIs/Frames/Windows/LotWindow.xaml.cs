using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// LotWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LotWindow : Window
    {
        private LotViewModel viewModel;

        public LotWindow()
        {
            InitializeComponent();

            this.viewModel = (LotViewModel)this.DataContext;
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void dataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double threshold = 30.0;

            if (e.VerticalOffset >= e.ExtentHeight - e.ViewportHeight - threshold)
            {
                var viewModel = DataContext as LotViewModel;
                if (viewModel != null && viewModel.LoadMoreDataCommand.CanExecute(null))
                {
                    viewModel.LoadMoreDataCommand.Execute(null);
                }
            }
        }

        private void dataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = $"{this.viewModel.FilePath}lot_statistics_{DateTime.Now:yyyy-MM-dd}_{this.viewModel.LotDataGridItemsSource[this.viewModel.LotDataGridSelectedIndex].LotNumber}.txt";
            this.viewModel.SaveStatistic();

            System.Diagnostics.Process.Start("Notepad.exe", fileName);
        }
    }
}
