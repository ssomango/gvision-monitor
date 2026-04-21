namespace GVisionWpf.Illuminations.Serials
{
    public interface ILvsEn08Serial : ILightSerial
    {
        public void SetBrightness(byte brightness, byte channel);

        public void TurnOff(byte channel);

    }
}
