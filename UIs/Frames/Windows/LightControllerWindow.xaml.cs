using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// LightControllerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LightControllerWindow : Window
    {
        private readonly LightViewModel viewModel = LightViewModel.Instance;

        public LightControllerWindow()
        {
            InitializeComponent();

            this.DataContext = this.viewModel;
            ((LightViewModel)this.DataContext).LoadRecipe();
            this.viewModel.SelectedIndex = 0;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.viewModel.LightControllerCollection.Clear();

            this.viewModel.LoadDataGridItems();

            if (this.viewModel.DataGridItems.Count == 0) { return; }

            this.viewModel.SelectedIndex = 0;
        }
    }
}
