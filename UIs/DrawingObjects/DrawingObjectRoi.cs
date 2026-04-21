using GVisionWpf.UIs.Frames.Panels;

namespace GVisionWpf.UIs.DrawingObjects
{
    public class DrawingObjectRoi : DrawingObject
    {
        private readonly Roi roi;

        public Roi Roi
        {
            get => this.roi;
        }

        public DrawingObjectRoi(Roi roi, EColor color) : base(color)
        {
            this.roi = roi;
        }

        public DrawingObjectRoi(Roi roi, VisionWindow window, EColor color) : base(window, color)
        {
            this.roi = roi;
        }

        public new void Create()
        {
            base.Create();
            UpdateGlobalTrackerTarget();
        }

        protected override HTuple CreateDrawingObject()
        {
            HOperatorSet.CreateDrawingObjectRectangle1(this.roi.Row1, this.roi.Col1, this.roi.Row2, this.roi.Col2, out HTuple drawingObjectId);
            return drawingObjectId;
        }

        public void UpdateGlobalTrackerTarget()
        {
            if (this.Window == null)
            {
                return;
            }
            GlobalTrackerTarget.Instance.SelectedDrawingObjectId = this.DrawingObjectId;
            GlobalTrackerTarget.Instance.HalconWindowId = this.Window!.xHSmartWindow.HalconWindow.Handle;
            GlobalTrackerTarget.Instance.DrawingObjectCallback = DrawingObjectCallback;
        }

        protected override void DrawingObjectCallbackHandler(IntPtr drawId, IntPtr windowHandle, string type)
        {
            HTuple values = GetParameter(new HTuple("row1", "column1", "row2", "column2"));
            this.roi.Row1 = values[0].D;
            this.roi.Col1 = values[1].D;
            this.roi.Row2 = values[2].D;
            this.roi.Col2 = values[3].D;

            UpdateGlobalTrackerTarget();
            OnDrawingObjectChanged();
        }
    }
}