using GVisionWpf.UIs.Frames.Panels;

namespace GVisionWpf.UIs.DrawingObjects
{
    public class DrawingObjectText : DrawingObject
    {
        private string text;
        private Point point;

        #region Property

        public string Text
        {
            get { return this.text; }
            set
            {
                this.text = value;
                SetParameter("string", value);
            }
        }

        public Point Point
        {
            get { return this.point; }
            set
            {
                this.point = value;
                SetParameter(new HTuple("row", "column"), new HTuple(value.Row, value.Col));
            }
        }

        #endregion

        public DrawingObjectText(string text, Point point, EColor color) : base(color)
        {
            this.text = text;
            this.point = point;
        }

        public DrawingObjectText(string text, Point point, VisionWindow window, EColor color) : base(window, color)
        {
            this.text = text;
            this.point = point;
        }

        protected override HTuple CreateDrawingObject()
        {
            HOperatorSet.CreateDrawingObjectText(Point.Row, Point.Col, Text, out HTuple drawingObjectId);
            HOperatorSet.SetDrawingObjectParams(drawingObjectId, "font", "default-20");
            return drawingObjectId;
        }

        protected override void DrawingObjectCallbackHandler(IntPtr drawId, IntPtr windowHandle, string type)
        {
            HTuple values = GetParameter(new HTuple("row", "column"));
            this.point.Row = values[0].D;
            this.point.Col = values[1].D;

            OnDrawingObjectChanged();
        }
    }
}