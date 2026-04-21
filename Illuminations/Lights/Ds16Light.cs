using GVisionWpf.Exceptions;
using GVisionWpf.Illuminations.Serials;

namespace GVisionWpf.Illuminations.Lights
{
    public class Ds16Light : Light
    {
        private IDs16Serial serial;

        public Ds16Light(IDs16Serial serial, string name, byte channel, int brightness, int maxBrightness, bool isInterlocked, string interlockGroup) : base(name, channel, brightness, maxBrightness, isInterlocked, interlockGroup)
        {
            this.serial = serial;
            this.SetBrightness(brightness);
        }

        public override void SetBrightness(int brightness)
        {
            if (brightness > 999)
            {
                throw new WrongValueException(); // Todo: GvisionException만들기
            }

            this.serial.SetBrightness((byte)brightness, this.Channel);
            this.Brightness = (byte)brightness;
        }

        public override void TurnOff()
        {
            // Strobe는 on-time 설정만 함으로 끌게 없음
            return;
        }
    }
}

