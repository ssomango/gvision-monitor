using GVisionWpf.UIs.Frames.Panels;

namespace GVisionWpf.UIs.DrawingObjects
{
    public class DrawingObjectRoiWithText : DrawingObjectRoi
    {
        private HDrawingObject.HDrawingObjectCallback DrawingObjectRoiCallBack { get; }
        private HTuple? drawingObjectText;

        public DrawingObjectRoiWithText(Roi roi, EColor color) : base(roi, color)
        {
            DrawingObjectRoiCallBack = DrawingObjectCallbackHandler;
        }

        public DrawingObjectRoiWithText(Roi roi, VisionWindow window, EColor color) : base(roi, window, color)
        {
            DrawingObjectRoiCallBack = DrawingObjectCallbackHandler;
        }

        public new void Create()
        {
            // TODO: ROI NAME은 NOT NULL이에요 티칭파일땜에 잠깐만 이렇게 합니다
            if (Roi.Name == null || Roi.Name == "") { Roi.Name = "ROI"; }

            base.Create();
            HOperatorSet.CreateDrawingObjectText(Roi.Row1, Roi.Col1, Roi.Name, out this.drawingObjectText);
            HOperatorSet.SetDrawingObjectParams(this.drawingObjectText, "color", ColorConverter.ToString(Color));
            UpdateTextPosition();
        }

        public new void Delete()
        {
            HOperatorSet.ClearDrawingObject(this.drawingObjectText);
            this.drawingObjectText = null;
            base.Delete();
        }

        public new void Attach()
        {
            if (this.drawingObjectText == null)
            {
                return;
            }

            HOperatorSet.AttachDrawingObjectToWindow(VisionWindow?.xHSmartWindow.HalconWindow, this.drawingObjectText);
            base.Attach();
        }

        public new void Detach()
        {
            if (this.drawingObjectText == null)
            {
                return;
            }

            HOperatorSet.DetachDrawingObjectFromWindow(VisionWindow?.xHSmartWindow.HalconWindow, this.drawingObjectText);
            base.Detach();
        }

        protected override void DrawingObjectCallbackHandler(IntPtr drawId, IntPtr windowHandle, string type)
        {
            UpdateTextPosition();
            base.DrawingObjectCallbackHandler(drawId, windowHandle, type);
        }

        public void UpdateTextPosition()
        {
            HTuple values = GetParameter(new HTuple("row1", "column1"));

            HOperatorSet.SetDrawingObjectParams(this.drawingObjectText, "row", values[0]);
            HOperatorSet.SetDrawingObjectParams(this.drawingObjectText, "column", values[1]);
        }
    }
}