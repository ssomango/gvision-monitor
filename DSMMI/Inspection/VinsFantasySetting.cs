namespace GVisionWpf.DSMMI.Inspection
{
    class VinsFantasySetting
    {
        private static readonly Lazy<VinsFantasySetting> lazy = new Lazy<VinsFantasySetting>(() => new VinsFantasySetting());
        public static VinsFantasySetting Instance => lazy.Value;

        public int Delay { get; set; } = 1000;
    }
}
