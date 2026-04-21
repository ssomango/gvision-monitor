using System.IO.Ports;

namespace GVisionWpf.Illuminations.Serials
{
    public class LFineSerial : ILFineSerial
    {
        private SerialPort port;

        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const byte BRIGHT_COMMAND = (byte)'w';
        private const byte ON_COMMAND = (byte)'o';
        private const byte OFF_COMMAND = (byte)'f';

        public LFineSerial(string comPort, int baudRate)
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

        public void SetBrightness(byte channel, int brightness)
        {
            string brightnessInString = brightness.ToString("D4");
            this.port.Write($"{(char)STX}{(char)channel}{(char)BRIGHT_COMMAND}{brightnessInString}{(char)ETX}");
        }

        public void TurnOn(byte channel)
        {
            this.port.Write($"{(char)STX}{(char)channel}{(char)OFF_COMMAND}{(char)ETX}");
        }

        public void TurnOff(byte channel)
        {
            this.SetBrightness(channel, 0);
        }
    }
}
