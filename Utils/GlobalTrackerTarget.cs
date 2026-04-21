namespace GVisionWpf.Utils
{
    public class GlobalTrackerTarget
    {
        private static readonly Lazy<GlobalTrackerTarget> lazy = new Lazy<GlobalTrackerTarget>(() => new GlobalTrackerTarget());
        public static GlobalTrackerTarget Instance => lazy.Value;

        public HTuple? HalconWindowId { get; set; } = null;
        public HTuple? SelectedDrawingObjectId { get; set; } = null;
        public HDrawingObject.HDrawingObjectCallback? DrawingObjectCallback { get; set; }

        private GlobalTrackerTarget() { }

        public void InvokeCallBack()
        {
            if (SelectedDrawingObjectId == null || SelectedDrawingObjectId.H == IntPtr.Zero || HalconWindowId == null)
            {
                return;
            }

            IntPtr drawId = SelectedDrawingObjectId.H;
            IntPtr windowHandle = HalconWindowId;
            DrawingObjectCallback?.Invoke(drawId, windowHandle, "on_select");
        }
    }
}