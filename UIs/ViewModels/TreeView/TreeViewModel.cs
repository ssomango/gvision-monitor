namespace GVisionWpf.UIs.ViewModels.TreeView
{
    public class TreeViewModel : ViewModelBase
    {
        private Tree tree;

        public Tree Tree
        {
            get => this.tree;
            set
            {
                this.tree = value;
                OnPropertyChanged();
            }
        }

        public TreeViewModel()
        {
            this.tree = new Tree(new NodeInfoViewModel() { Name = "Root" });
        }
    }
}
