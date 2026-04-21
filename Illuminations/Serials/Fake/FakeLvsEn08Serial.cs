namespace GVisionWpf.Illuminations.Serials.Fake
{
    public class FakeLvsEn08Serial : ILvsEn08Serial
    {

        public FakeLvsEn08Serial()
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

        public void SetBrightness(byte brightness, byte channel)
        {
            return;
        }

        public void TurnOff(byte channel)
        {
            return;
        }

        private void setChannel(byte channel)
        {
            return;
        }

        private void Send(byte opCode, byte length, byte address, byte data)
        {
            return;
        }
    }


}
