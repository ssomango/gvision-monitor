using GVisionWpf.Exceptions;
using GVisionWpf.Illuminations.Serials;

namespace GVisionWpf.Illuminations.Lights
{
    public class LFineLight : Light
    {
        ILFineSerial serial;

        public LFineLight(ILFineSerial serial, string name, byte channel, int brightness, int maxBrightness, bool isInterlocked, string interlockGroup) : base(name, channel, brightness, maxBrightness, isInterlocked, interlockGroup)
        {
            this.serial = serial;
        }

        public override void SetBrightness(int brightness)
        {
            if (brightness > this.MaxBrightness)
            {
                throw new WrongValueException();
            }

            this.serial.SetBrightness(this.Channel, (byte)brightness);
            this.Brightness = (byte)brightness;
        }

        public override void TurnOff()
        {
            this.serial.TurnOff(this.Channel);
        }
    }
}

