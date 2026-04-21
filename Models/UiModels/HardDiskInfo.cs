namespace GVisionWpf.Models.UiModels
{
    public class HardDiskInfo
    {
        public string Name { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long FreeSize { get; set; }
        public long UseSize { get; set; }
    }
}