namespace GVisionWpf.Illuminations.Serials
{
    public interface ILvsEt04Serial : ILightSerial
    {
        public void SetBrightness(byte brightness, byte channel);

        public void TurnOff(byte channel);


    }
}
