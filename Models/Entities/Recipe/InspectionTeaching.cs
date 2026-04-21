using CommunityToolkit.Mvvm.ComponentModel;

namespace GVisionWpf.Models.Entities.Recipe
{
    public abstract partial class InspectionTeaching : ObservableValidator, IIdentifiable
    {
        public string TeachingId = "";
        public DateTime CreatedAt;

        public bool IsTaught;
        public string Id
        {
            get => this.TeachingId;
            set => this.TeachingId = value;
        }
    }
}