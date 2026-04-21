using GVisionWpf.UIs.Frames.Panels;

namespace GVisionWpf.UIs.DrawingObjects
{
    public class DrawingObjectLine : DrawingObject
    {
        private Point start;
        private Point end;

        #region Property

        public Point Start
        {
            get { return this.start; }
            set { this.start = value; }
        }
        public Point End
        {
            get { return this.end; }
            set { this.end = value; }
        }

        #endregion

        public DrawingObjectLine(Point start, Point end, EColor color) : base(color)
        {
            this.start = start;
            this.end = end;
        }

        public DrawingObjectLine(Point start, Point end, VisionWindow window, EColor color) : base(window, color)
        {
            this.start = start;
            this.end = end;
        }

        protected override HTuple CreateDrawingObject()
        {
            HOperatorSet.CreateDrawingObjectLine(this.start.Row, this.start.Col, this.end.Row, this.end.Col, out HTuple drawingObjectId);
            return drawingObjectId;
        }

        protected override void DrawingObjectCallbackHandler(IntPtr drawId, IntPtr windowHandle, string type)
        {
            HTuple values = GetParameter(new HTuple("row1", "column1", "row2", "column2"));
            this.start.Row = values[0].D;
            this.start.Col = values[1].D;
            this.end.Row = values[2].D;
            this.end.Col = values[3].D;

            OnDrawingObjectChanged();
        }
    }
}