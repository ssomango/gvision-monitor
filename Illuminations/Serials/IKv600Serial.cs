namespace GVisionWpf.Illuminations.Serials
{
    public interface IKv600Serial : ILightSerial
    {

        public void SetBrightness(byte brightness, byte channel);

        public void TurnOff(byte channel);

    }
}
