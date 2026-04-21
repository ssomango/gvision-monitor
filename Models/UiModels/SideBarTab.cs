using System.Collections.ObjectModel;

namespace GVisionWpf.Models.UiModels
{
    public class SidebarTab
    {
        public string TabName { get; set; } = string.Empty;
        public bool IsExpanded { get; set; }
        public ObservableCollection<SidebarButton> Buttons { get; set; } = new ObservableCollection<SidebarButton>();
    }
}