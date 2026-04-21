using GVisionWpf.Cameras.CameraTypes;

namespace GVisionWpf.GlobalStates
{
    internal class CameraInfo
    {
        public ECameraInterface CameraInterface { get; set; }
        public ECamera CameraType { get; set; }
        public ECameraMode LiveMode { get; set; }
        public ECameraMode RunMode { get; set; }

        public ECameraTriggerMode CameraTriggerMode { get; set; }

        public bool HorizontalFlip { get; set; }
        public bool VerticalFlip { get; set; }
        public double PixelPerMillimeter { get; set; }
        public int? MaxDelay { get; set; }
        public Dictionary<string, object>? Params { get; set; }
    }
}
