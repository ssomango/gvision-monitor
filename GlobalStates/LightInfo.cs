using GVisionWpf.Illuminations;

namespace GVisionWpf.GlobalStates
{
    public class LightInfo
    {
        public ELight LightType { get; set; }
        public string ControllerName { get; set; }
        public int Channel { get; set; }
        public int Brightness { get; set; }
        public int MaxBrightness { get; set; }
        public string LightName { get; set; }
        public bool IsInterlocked { get; set; }
        public string InterlockGroup { get; set; }
    }
}
