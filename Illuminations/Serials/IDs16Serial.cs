namespace GVisionWpf.Illuminations.Serials
{
    public interface IDs16Serial : ILightSerial
    {

        public void SetBrightness(byte brightness, byte channel);

        public void TurnOff(byte channel);

        public void Save();

        public void Trigger();
    }
}
