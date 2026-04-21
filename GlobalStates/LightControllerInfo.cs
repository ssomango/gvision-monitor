using GVisionWpf.Illuminations;

namespace GVisionWpf.GlobalStates
{
    public class LightControllerInfo
    {
        public string ComPort { get; set; }

        public int BaudRate { get; set; }

        public ELightInterface LightInterface { get; set; }

        public string ControllerName { get; set; }
    }
}
