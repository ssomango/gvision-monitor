namespace GVisionWpf.Illuminations.Serials.Fake
{
    public class FakeKv600Serial : IKv600Serial
    {

        public FakeKv600Serial()
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

        private void Send(byte brightness, byte channel)
        {
            return;
        }
    }


}
