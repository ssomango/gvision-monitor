using System.IO.Ports;

namespace GVisionWpf.Illuminations.Serials
{
    public class Ds16Serial : IDs16Serial
    {
        private SerialPort port;

        private const byte START = 0x3A;
        private const byte END_CR = 0x0D;
        private const byte END_LF = 0x0A;
        private const byte DEV_CODE = 0x00;
        private const byte ON_TIME_OP_CODE = 0x42;
        private const byte SAVE_OP_CODE = 0x53;
        private const byte TRIG_OP_CODE = 0x54;


        public int[] onTimes = new int[16];


        public Ds16Serial(string comPort, int baudRate)
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
            const byte MAX_PAGE = 1;
            const byte PAGE = 0;
            const byte RP = 1; // 스트로브 반복횟수

            this.onTimes[channel] = brightness;

            string command = $"{(char)START}00{(char)ON_TIME_OP_CODE}0100{RP}";

            List<string> timeList = new List<string>();
            foreach (int t in this.onTimes)
            {
                timeList.Add(t.ToString("D3"));
            }
            string times = string.Join(",", timeList);
            command += times;

            command += $"{(char)END_CR}{(char)END_LF}";

            this.port.Write(command);
        }

        public void TurnOff(byte channel)
        {
            return;
        }

        public void Save()
        {
            string command = $"{(char)START}00{(char)SAVE_OP_CODE}{(char)END_CR}{(char)END_LF}";
            this.port.Write(command);
        }

        public void Trigger()
        {
            string command = $"{(char)START}00{(char)TRIG_OP_CODE}PS{(char)END_CR}{(char)END_LF}";
            this.port.Write(command);
        }

    }


}
