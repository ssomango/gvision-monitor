using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Data.TeachingModel;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.UIs.Frames.Panels;

namespace GVisionWpf.UIs.ViewModels.Teaching
{
    public abstract partial class SingleTeachingViewModelBase<TTeaching> : ViewModelBase
        where TTeaching : InspectionTeaching, ISinglePackageTeachingModel<TTeaching>
    {
        [ObservableProperty]
        private VisionWindow visionWindow;

        [ObservableProperty]
        private HObject teachingImage;

        [ObservableProperty]
        private TTeaching teaching;

        [ObservableProperty]
        private string inspectionResultStr;


        public void ClearImage()
        {
            VisionWindow?.Clear();
            VisionWindow?.Display(TeachingImage);
        }
    }
}
