using System.Windows.Input;

namespace GVisionWpf.Models.UiModels
{
    public class SidebarButton
    {
        public string Name { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string CommandName { get; set; } = string.Empty;
        public bool IsRunModeAllowed { get; set; } = true;
        public bool IsSetUpModeAllowed { get; set; } = true;
        public ICommand? Command { get; set; } = null;
    }
}