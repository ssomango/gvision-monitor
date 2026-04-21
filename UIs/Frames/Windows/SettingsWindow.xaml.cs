using GVisionWpf.UIs.ViewModels;
using GVisionWpf.UIs.ViewModels.TreeView;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// SettingsWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            DataContext = SettingsViewModel.Instance;
        }

        #region Events

        private void exitBtn_Onclick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SettingsViewModel.Instance.SelectedNode = e.NewValue as TreeNodeViewModel;
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string newText = ((TextBox)sender).Text + e.Text;

            if (!uint.TryParse(newText, out _))
            {
                e.Handled = true;
            }
        }
    }
}