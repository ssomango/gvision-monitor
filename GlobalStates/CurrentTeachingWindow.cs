using GVisionWpf.UIs.Frames.Panels;

namespace GVisionWpf.GlobalStates
{
    public class CurrentTeachingWindow
    {
        private static readonly Lazy<CurrentTeachingWindow> lazy = new Lazy<CurrentTeachingWindow>(() => new CurrentTeachingWindow());
        public static CurrentTeachingWindow Instance => lazy.Value;

        public VisionWindow? Window { get; set; }
        public EInspection InspectionType { get; set; } = EInspection.Qfn;
        public HObject? TeachingImage { get; set; }
    }
}