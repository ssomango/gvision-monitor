namespace GVisionWpf.Illuminations.Serials
{
    public interface ILFineSerial : ILightSerial
    {

        public void SetBrightness(byte channel, int brightness);

        public void TurnOn(byte channel);

        public void TurnOff(byte channel);

    }
}
