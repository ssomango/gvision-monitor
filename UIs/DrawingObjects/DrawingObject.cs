using GVisionWpf.GlobalStates;
using GVisionWpf.UIs.Frames.Panels;
using System.Runtime.InteropServices;

namespace GVisionWpf.UIs.DrawingObjects
{
    public abstract class DrawingObject
    {
        private readonly EColor color;
        protected HTuple? DrawingObjectId;
        protected VisionWindow? Window;
        protected HDrawingObject.HDrawingObjectCallback DrawingObjectCallback { get; }
        public event EventHandler? DrawingObjectChanged;

        protected abstract void DrawingObjectCallbackHandler(IntPtr drawId, IntPtr windowHandle, string type);
        protected abstract HTuple CreateDrawingObject();

        public VisionWindow? VisionWindow
        {
            get => this.Window;
            set => this.Window = value;
        }

        public EColor Color
        {
            get => this.color;
        }

        protected DrawingObject(EColor color) : this(CurrentTeachingWindow.Instance.Window!, color) { }

        protected DrawingObject(VisionWindow window, EColor color)
        {
            this.Window = window;
            this.color = color;
            DrawingObjectCallback = DrawingObjectCallbackHandler;
        }

        public void Create()
        {
            this.DrawingObjectId = CreateDrawingObject();
            SetParameter("color", ColorConverter.ToString(this.color));

            IntPtr dragObjectCallbackPtr = Marshal.GetFunctionPointerForDelegate(DrawingObjectCallback!);
            HTuple objectEvent = new HTuple("on_resize", "on_drag", "on_select");
            HOperatorSet.SetDrawingObjectCallback(this.DrawingObjectId, objectEvent, dragObjectCallbackPtr);
        }

        public void Attach()
        {
            if (this.Window == null || this.DrawingObjectId == null)
            {
                return;
            }

            HWindow? window = this.Window?.xHSmartWindow.HalconWindow;
            HOperatorSet.AttachDrawingObjectToWindow(window, this.DrawingObjectId);
        }

        public void Detach()
        {
            if (this.DrawingObjectId == null) { return; }

            HWindow? window = this.Window?.xHSmartWindow.HalconWindow;
            HOperatorSet.DetachDrawingObjectFromWindow(window, this.DrawingObjectId);
        }

        public void Delete()
        {
            if (this.DrawingObjectId == null) { return; }

            HWindow? window = this.Window?.xHSmartWindow.HalconWindow;
            HOperatorSet.ClearDrawingObject(this.DrawingObjectId);
            this.DrawingObjectId = null;
            DrawingObjectChanged = null;
        }

        public void SetParameter(HTuple paramName, HTuple paramValue)
        {
            try
            {
                HOperatorSet.SetDrawingObjectParams(this.DrawingObjectId, paramName, paramValue);
            }
            catch
            {
                // ignored
            }
        }

        public HTuple GetParameter(HTuple paramName)
        {
            HOperatorSet.GetDrawingObjectParams(this.DrawingObjectId, paramName, out HTuple paramValue);
            return paramValue;
        }

        protected virtual void OnDrawingObjectChanged()
        {
            DrawingObjectChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}