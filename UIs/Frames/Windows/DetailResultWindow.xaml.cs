using GVisionWpf.UIs.ViewModels;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GVisionWpf.UIs.Frames.Windows
{
    /// <summary>
    /// DetailResultWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DetailResultWindow : FloatingWindowBase
    {
        public DetailResultWindow(DetailResultViewModel detailResultViewModel) : base("Detail Inspection Result")
        {
            InitializeComponent();
            DataContext = detailResultViewModel;
            detailResultViewModel.PropertyChanged += viewModel_PropertyChanged;
        }

        // RichTextBox의 content인 Document는 바인딩이 불가능하여 따로 처리
        private void viewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            if (e.PropertyName == nameof(DetailResultViewModel.Description))
            {
                setRichTextBoxContent(((DetailResultViewModel)sender).Description);
            }
        }

        private void setRichTextBoxContent(string content)
        {
            this.xRichTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph();
            string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (line.Contains("(NG)"))
                {
                    paragraph.Inlines.Add(new Run(line + Environment.NewLine)
                    {
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold
                    });
                }
                else
                {
                    paragraph.Inlines.Add(new Run(line + Environment.NewLine));
                }
            }

            this.xRichTextBox.Document.Blocks.Add(paragraph);
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
