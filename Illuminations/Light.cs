namespace GVisionWpf.Illuminations
{
    public abstract class Light
    {
        public string Name { get; }
        public byte Channel;
        public int MaxBrightness;
        public int Brightness;
        public bool IsInterlocked;
        public string InterlockGroup;

        public Light() { }

        public Light(string name, byte channel, int brightness, int maxBrightness, bool isInterlocked, string interlockGroup)
        {
            this.Name = name;
            this.Channel = channel;
            this.Brightness = brightness;
            this.MaxBrightness = maxBrightness;
            this.IsInterlocked = isInterlocked;
            this.InterlockGroup = interlockGroup;
        }

        public abstract void SetBrightness(int brightness);

        public abstract void TurnOff();
    }
}
