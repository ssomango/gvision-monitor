using GVisionWpf.Exceptions;
using GVisionWpf.Illuminations.Serials;

namespace GVisionWpf.Illuminations.Lights
{
    public class LvsEn08Light : Light
    {
        ILvsEn08Serial serial;

        public LvsEn08Light(ILvsEn08Serial serial, string name, byte channel, int brightness, int maxBrightness, bool isInterlocked, string interlockGroup) : base(name, channel, brightness, maxBrightness, isInterlocked, interlockGroup)
        {
            this.serial = serial;
        }

        public override void SetBrightness(int brightness)
        {
            if (brightness > this.MaxBrightness)
            {
                throw new WrongValueException(); // Todo: GvisionException만들기
            }
            this.serial.SetBrightness((byte)brightness, this.Channel);
            this.Brightness = (byte)brightness;
        }

        public override void TurnOff()
        {
            this.serial.TurnOff(this.Channel);
        }
    }
}

