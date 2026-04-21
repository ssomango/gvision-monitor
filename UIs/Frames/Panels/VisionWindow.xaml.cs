using GVisionWpf.Events.Message;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.UIs.DrawingObjects;
using GVisionWpf.Visions;
using System.Windows;
using System.Windows.Controls;
using static HalconDotNet.HDrawingObject;
using Point = GVisionWpf.Models.Visions.Point;

namespace GVisionWpf.UIs.Frames.Panels
{
    /// <summary>
    /// VisionWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class VisionWindow : UserControl
    {
        private bool shouldShowGrayValue = false;
        private bool shouldUseRuler = false;
        private bool shouldUseLineProfile = false;

        private bool isFirstImage = true;
        private HObject? currentImage;

        private DrawingObjectLine? drawingObjectLine;

        private HDrawingObjectCallback DrawingObjectCallback { get; set; }

        private bool isWindowLoaded;
        public bool IsWindowLoaded => this.isWindowLoaded;

        public VisionWindow()
        {
            InitializeComponent();
            Loaded += onLoaded;
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            this.xHSmartWindow.HalconWindow.SetWindowParam("graphics_stack_max_element_num", "unlimited");
            this.xHSmartWindow.HalconWindow.SetWindowParam("graphics_stack_max_memory_size", "unlimited");
            this.isWindowLoaded = true;
        }

        public void ZoomImage(int delta)
        {
            if (this.currentImage == null) { return; }

            System.Windows.Size windowSize = this.xHSmartWindow.WindowSize;

            double x = windowSize.Width / 2;
            double y = windowSize.Height / 2;

            this.xHSmartWindow.HZoomWindowContents(x, y, delta);
        }

        public void SetFullImagePart()
        {
            Application.Current.Dispatcher.Invoke(() => { this.xHSmartWindow.SetFullImagePart(); });
        }

        public void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.xHSmartWindow.HalconWindow.ClearWindow();
            });
        }

        public void Display(HObject? image)
        {
            Clear();
            if (image == null || image.Key == IntPtr.Zero) { return; }

            this.currentImage = image;
            Application.Current.Dispatcher.Invoke(() => { this.xHSmartWindow.HalconWindow.DispObj(image); });
        }

        public void Display(HObject region, EColor color, string drawMode = "margin", int lineWidth = 1)
        {
            Display(region, ColorConverter.ToString(color), drawMode, lineWidth);
        }

        public void Display(HObject? region, string color, string drawMode = "margin", int lineWidth = 1)
        {
            if (region == null || region.Key == IntPtr.Zero) { return; }

            Application.Current.Dispatcher.Invoke(() =>
            {
                this.xHSmartWindow.HalconWindow.SetDraw(drawMode);
                this.xHSmartWindow.HalconWindow.SetLineWidth(lineWidth);
                this.xHSmartWindow.HalconWindow.SetColor(color);
                this.xHSmartWindow.HalconWindow.DispObj(region);
            });
        }

        private void display(string text, int row, int col, string color = "white", ECoordinateSystem coordinate = ECoordinateSystem.Image, string font = "default-14", bool box = false, string boxColor = "green")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.xHSmartWindow.HalconWindow.SetFont(font);
                this.xHSmartWindow.HalconWindow.SetColor(color);
                this.xHSmartWindow.HalconWindow.DispText(text, coordinate.ToString().ToLower(), row, col, color, new HTuple("box", "box_color"), new HTuple(box.ToString().ToLower(), boxColor));
            });
        }

        public void Display(List<FloatingText> texts)
        {
            foreach (FloatingText text in texts)
            {
                Display(text);
            }
        }

        public void Display(FloatingText text)
        {
            display(text.Content, (int)text.Point.Row, (int)text.Point.Col, ColorConverter.ToString(text.Color), ECoordinateSystem.Image, text.Font);
        }

        public void Display(List<FixedText> texts)
        {
            if (texts.Count == 0) { return; }

            string totalText = texts.Aggregate(string.Empty, (current, text) => current + (text.Content + '\n'));
            display(totalText, 5, 5, ColorConverter.ToString(texts.First().Color), ECoordinateSystem.Window, texts.First().Font);
        }

        public void Display(FixedText text)
        {
            display(text.Content, 5, 5, ColorConverter.ToString(text.Color), ECoordinateSystem.Window, text.Font);
        }

        public void Display(List<RenderableInspectionResult> renderables)
        {
            Clear();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Display(renderables.First().InspectionResult.Image);

                if (this.isFirstImage)
                {
                    SetFullImagePart();
                    this.isFirstImage = false;
                }

                renderables.ForEach(r =>
                {
                    r.RenderData.ResultDrawings.ForEach(elem => Display(elem.drawingObject, elem.color));

                    Display(r.RenderData.FloatingTexts);
                    Display(r.RenderData.FixedTexts);
                });
            });
        }

        public void Display(RenderableInspectionResult renderable)
        {
            Clear(); 

            Application.Current.Dispatcher.Invoke(() =>
            {
                Display(renderable.InspectionResult.Image);

                if (this.isFirstImage)
                {
                    SetFullImagePart();
                    this.isFirstImage = false;
                }

                renderable.RenderData.ResultDrawings.ForEach(elem => Display(elem.drawingObject, elem.color));
                Display(renderable.RenderData.FixedTexts);
                Display(renderable.RenderData.FloatingTexts);
            });
        }

        public void ToggleGrayValueMode()
        {
            this.shouldShowGrayValue = !this.shouldShowGrayValue;
            Display(this.currentImage);

            if (!this.shouldShowGrayValue)
            {
                return;
            }

            this.shouldUseRuler = false;
            this.shouldUseLineProfile = false;

            this.drawingObjectLine?.Detach();
            this.drawingObjectLine?.Delete();
        }

        private void xHSmartWindow_OnHMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!this.shouldShowGrayValue)
            {
                return;
            }

            try
            {
                this.xHSmartWindow.HalconWindow.GetMposition(out int row, out int col, out _);
                HOperatorSet.GetGrayval(this.currentImage, row, col, out HTuple grayValue);

                Display(this.currentImage);
                Display(new FixedText($"Gray Value: {grayValue}", 1, EColor.Green, 22));
            }
            catch
            {
                // ignored
            }
        }

        public void ToggleRulerMode()
        {
            this.shouldUseRuler = !this.shouldUseRuler;

            Display(this.currentImage);

            this.drawingObjectLine?.Detach();
            this.drawingObjectLine?.Delete();

            if (this.shouldUseRuler)
            {
                this.shouldShowGrayValue = false;
                this.shouldUseLineProfile = false;

                VisionOperation.GetCenterPointOfRegion(this.currentImage!, out Point centerPoint);
                Point start = centerPoint - new Point(0, 400);
                Point end = centerPoint + new Point(0, 400);

                this.drawingObjectLine = new DrawingObjectLine(start, end, EColor.Green);
                this.drawingObjectLine.Create();
                this.drawingObjectLine.Attach();
                this.drawingObjectLine.DrawingObjectChanged += onRulerDrawingObjectChanged;
            }
        }

        public void ToggleLineProfileMode()
        {
            this.shouldUseLineProfile = !this.shouldUseLineProfile;

            Display(this.currentImage);

            this.drawingObjectLine?.Detach();
            this.drawingObjectLine?.Delete();

            if (this.shouldUseLineProfile)
            {
                this.shouldShowGrayValue = false;
                this.shouldUseRuler = false;

                VisionOperation.GetCenterPointOfRegion(this.currentImage!, out Point centerPoint);
                Point start = centerPoint - new Point(0, 400);
                Point end = centerPoint + new Point(0, 400);

                this.drawingObjectLine = new DrawingObjectLine(start, end, EColor.Green);
                this.drawingObjectLine.Create();
                this.drawingObjectLine.Attach();
                this.drawingObjectLine.DrawingObjectChanged += onLineProfileDrawingObjectChanged;
            }
        }

        private void onRulerDrawingObjectChanged(object? sender, EventArgs e)
        {
            if (this.drawingObjectLine == null)
            {
                return;
            }

            Point start = this.drawingObjectLine.Start;
            Point end = this.drawingObjectLine.End;

            HOperatorSet.DistancePp(start.Row, start.Col, end.Row, end.Col, out HTuple pxDistance);
            double mmDistance = GlobalSetting.Instance.Inspection.LengthUnit.ConvertFromPixel(ECamera.PRS, pxDistance.D);

            Display(this.currentImage);
            Display(new FixedText($"Length: {mmDistance:F2}µm", 1, EColor.Green, 22));
        }

        private void onLineProfileDrawingObjectChanged(object? sender, EventArgs e)
        {
            if (this.drawingObjectLine == null)
            {
                return;
            }

            Point start = this.drawingObjectLine.Start;
            Point end = this.drawingObjectLine.End;

            VisionOperation.GenLineProfile(this.currentImage!, start, end, out HObject lineProfile);
            Display(this.currentImage);
            Display(lineProfile, EColor.Green, "fill");
        }
    }
}