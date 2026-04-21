using System.IO.Ports;

namespace GVisionWpf.Illuminations.Serials
{
    public class LvsEn08Serial : ILvsEn08Serial
    {
        private SerialPort port;

        private const byte START = 0x01;
        private const byte END = 0x04;


        public LvsEn08Serial(string comPort, int baudRate)
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

    public enum EOperation
    {
        Write = 0x00,
        Read = 0x01,
    }

    public enum EAddress
    {
        RTR = 0x00,
        RWTR = 0x04,
        CSR = 0x20,
        SVR = 0x28,
        SCR = 0x2C,
        RCR = 0x2D,
        SOR = 0x2E,
        SSR = 0x30,
        COR = 0x34,
        PCR = 0x38,
        PACR = 0x39,
        PASR = 0x3C,
        EEAR = 0x40,
        EEDR = 0x44,
        ECR = 0x4C,
        UTR = 0xC0,
        ETR = 0xC4,
        TTR = 0xD8
    }

}
