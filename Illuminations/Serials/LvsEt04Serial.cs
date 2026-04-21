using System.IO.Ports;

namespace GVisionWpf.Illuminations.Serials
{
    public class LvsEt04Serial : ILvsEt04Serial
    {
        private SerialPort port;

        private const byte START = 0x01;
        private const byte END = 0x04;


        public LvsEt04Serial(string comPort, int baudRate)
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
            setChannel(channel);
            Send((byte)EOperation.Write, 1, (byte)EAddress.SVR, brightness);
        }

        public void TurnOff(byte channel)
        {
            SetBrightness(0, channel); // 밝기를 0으로 설정하는것이 재점등시 반응성이 좋다고 합니다.

            //this.Send((byte)EOperation.Write, 1, (byte)EAddress.COR, channels); 
            // e.g. 0x01010101 0: off 1: on 앞에서부터 8채널~1채널
            // 이거쓸려면 channels로 받게 바꾸고
        }

        private void setChannel(byte channel)
        {
            Send((byte)EOperation.Write, 1, (byte)EAddress.CSR, channel);
        }

        private void Send(byte opCode, byte length, byte address, byte data)
        {
            byte[] command = new byte[] { START, opCode, length, address, data, END };

            this.port.Write(command, 0, command.Length);
            this.port.Write($"{START}{length}{address}{data}{END}");
        }
    }


}
