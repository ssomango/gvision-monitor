namespace GVisionWpf.Illuminations.Serials.Fake
{
    public class FakeLvsEt04Serial : ILvsEt04Serial
    {

        public FakeLvsEt04Serial()
        {

        }


        public new void OpenPort()
        {
            return;
        }

        public new void ClosePort()
        {
            return;
        }

        public new void SetBrightness(byte brightness, byte channel)
        {
            return;
        }

        public new void TurnOff(byte channel)
        {
            return;
        }

        private new void setChannel(byte channel)
        {
            return;
        }

        private new void Send(byte opCode, byte length, byte address, byte data)
        {
            return;
        }
    }


}
