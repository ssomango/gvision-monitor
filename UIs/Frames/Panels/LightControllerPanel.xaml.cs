using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// LightControllerPanel2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LightControllerPanel : UserControl
    {
        public LightControllerPanel()
        {
            InitializeComponent();
        }

        private void lightTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
