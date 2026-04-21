using System.Collections.ObjectModel;

namespace GVisionWpf.Models.UiModels
{
    public class MainWindowLayout
    {
        public string TitleName;
        public string LogoImageName;
        public ObservableCollection<SidebarTab> SidebarTabs;
    }
}