using System.Windows.Controls;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// InspectionResultPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InspectionResultPanel : UserControl
    {
        private InspectionResultModel model;

        public InspectionResultPanel()
        {
            InitializeComponent();

            this.model = new InspectionResultModel();

            DataContext = this.model;
        }

        public InspectionResultPanel(string name)
        {
            InitializeComponent();

            this.model = new InspectionResultModel(name);

            DataContext = this.model;
        }
    }

    public class InspectionResultModel
    {
        public string Name { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int ResultCount { get; set; } = 0;

        public InspectionResultModel() { }

        public InspectionResultModel(string name)
        {
            Name = name;
        }
    }
}
