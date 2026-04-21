namespace GVisionWpf.UIs.ViewModels.TreeView
{
    public class NodeInfoViewModel : ViewModelBase
    {
        private string name;

        public string Name
        {
            get => this.name;
            set
            {
                this.name = value;
                OnPropertyChanged();
            }
        }

        public NodeInfoViewModel()
        {
            this.name = "";
        }

    }
}
