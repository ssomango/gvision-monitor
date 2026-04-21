using GVisionWpf.Illuminations;

namespace GVisionWpf.UIs.ViewModels
{
    public class LightControllerPanelViewModel : ViewModelBase
    {
        private ELight light;
        private string lightName;
        private int brightness;
        private int maximum;
        private bool isInterlocked;
        private string interlockGroup;

        #region Property

        public ELight Light
        {
            get => this.light;
            set
            {
                this.light = value;
                OnPropertyChanged();
            }
        }

        public string LightName
        {
            get => this.lightName;
            set
            {
                this.lightName = value;
                OnPropertyChanged();
            }
        }

        public int Brightness
        {
            get => this.brightness;
            set
            {
                this.brightness = value;
                OnPropertyChanged();
            }
        }

        public int Maximum
        {
            get => this.maximum;
            set
            {
                this.maximum = value;
                OnPropertyChanged();
            }
        }

        public bool IsInterlocked
        {
            get => this.isInterlocked;
            set
            {
                this.isInterlocked = value;
                OnPropertyChanged();
            }
        }

        public string InterlockGroup
        {
            get => this.interlockGroup;
            set
            {
                this.interlockGroup = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public LightControllerPanelViewModel() { }

        public LightControllerPanelViewModel(ELight light, string lightName, int brightness, int maximum, bool isInterlocked, string interlockGroup)
        {
            Light = light;
            LightName = lightName;
            Brightness = brightness;
            Maximum = maximum;
            IsInterlocked = isInterlocked;
            InterlockGroup = interlockGroup;
        }
    }
}
