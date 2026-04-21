using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Interfaces;

namespace GVisionWpf.Models.Visions
{
    public partial class Threshold : ObservableObject, ICopyable<Threshold>
    {
        [ObservableProperty]
        private int minGray = 0;

        [ObservableProperty]
        private int maxGray = 255;

        [ObservableProperty]
        private bool isAuto = false;

        public Threshold()
        {
            MinGray = 0;
            MaxGray = 255;
            isAuto = false;
        }

        public Threshold(int minGray, int maxGray, bool isAuto = false)
        {
            MinGray = minGray;
            MaxGray = maxGray;
            IsAuto = isAuto;
        }

        public Threshold(bool isAuto)
        {
            MinGray = 0;
            MaxGray = 255;
            IsAuto = isAuto;
        }

        public void CopyFrom(Threshold other)
        {
            MinGray = other.MinGray;
            MaxGray = other.MaxGray;
            IsAuto = other.IsAuto;
        }
    }
}
