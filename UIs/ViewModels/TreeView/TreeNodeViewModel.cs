using System.Collections.ObjectModel;

namespace GVisionWpf.UIs.ViewModels.TreeView
{
    public class TreeNodeViewModel : ViewModelBase
    {
        private NodeInfoViewModel data;
        private ObservableCollection<TreeNodeViewModel> children;
        private TreeNodeViewModel? parent;

        #region Property

        public NodeInfoViewModel Data
        {
            get => this.data;
            set
            {
                this.data = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TreeNodeViewModel> Children
        {
            get => this.children;
            set
            {
                this.children = value;
                OnPropertyChanged();
            }
        }
        public TreeNodeViewModel Parent
        {
            get => this.parent;
            set
            {
                this.parent = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public TreeNodeViewModel(NodeInfoViewModel data)
        {
            Data = data;
            this.children = new ObservableCollection<TreeNodeViewModel>();
        }

        public void AddChild(NodeInfoViewModel data)
        {
            TreeNodeViewModel child = new TreeNodeViewModel(data);
            child.Parent = this;
            this.children.Add(child);
            OnPropertyChanged();
        }

        public void RemoveChild(NodeInfoViewModel data)
        {
            var nodeToRemove = this.children.FirstOrDefault(c => EqualityComparer<NodeInfoViewModel>.Default.Equals(c.Data, data));
            if (nodeToRemove != null)
            {
                this.children.Remove(nodeToRemove);
            }
            OnPropertyChanged();
        }

        public TreeNodeViewModel FindNode(NodeInfoViewModel data)
        {
            if (EqualityComparer<NodeInfoViewModel>.Default.Equals(Data, data))
            {
                return this;
            }

            foreach (var child in Children)
            {
                var foundNode = child.FindNode(data);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }

            return null;
        }

        public TreeNodeViewModel FindNodeByName(string name)
        {
            if (Data.Name == name)
            {
                return this;
            }

            foreach (var child in Children)
            {
                var foundNode = child.FindNodeByName(name);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }

            return null;
        }

        public bool ContainsNode(TreeNodeViewModel selectedNode)
        {
            if (this == selectedNode)
            {
                return true;
            }

            foreach (var child in Children)
            {
                if (child.ContainsNode(selectedNode))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
