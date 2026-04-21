using System.IO.Ports;

namespace GVisionWpf.Illuminations.Serials
{
    public class Kv600Serial : IKv600Serial
    {
        private SerialPort port;

        private const byte START = (byte)'#';
        private const byte END = (byte)'&';


        public Kv600Serial(string comPort, int baudRate)
        {
            this.port = new SerialPort(comPort, baudRate);

            OpenPort();
        }


        public void OpenPort()
        {
            this.port.Parity = Parity.None;
            this.port.StopBits = StopBits.One;
            this.port.DataBits = 8;
            this.port.Encoding = System.Text.Encoding.ASCII;

            if (!this.port.IsOpen)
            {
                this.port.Open();
            }

        }

        public void ClosePort()
        {
            this.port.Close();
        }

        public void SetBrightness(byte brightness, byte channel)
        {
            Send(brightness, channel);
        }

        public void TurnOff(byte channel)
        {
            SetBrightness(0, channel);
        }

        private void Send(byte brightness, byte channel)
        {
            string command = $"{(char)START}A{channel}{brightness.ToString("D3")}{(char)END}";
            this.port.Write(command);
        }
    }


}
