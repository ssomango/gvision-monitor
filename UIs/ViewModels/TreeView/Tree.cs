namespace GVisionWpf.UIs.ViewModels.TreeView
{
    public class Tree
    {
        public TreeNodeViewModel Root { get; set; }
        public Tree(NodeInfoViewModel data) => Root = new TreeNodeViewModel(data);
    }
}
