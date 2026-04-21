namespace GVisionWpf.Illuminations.Serials.Fake
{
    public class FakeLFineSerial : ILFineSerial
    {
        public FakeLFineSerial()
        {

        }

        public void OpenPort()
        {
            return;
        }

        public void ClosePort()
        {
            return;
        }

        public void SetBrightness(byte channel, int brightness)
        {
            return;
        }

        public void TurnOn(byte channel)
        {
            return;
        }

        public void TurnOff(byte channel)
        {
            return;
        }
    }
}
