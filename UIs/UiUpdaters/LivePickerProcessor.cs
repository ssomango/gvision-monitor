using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories.Calibrations;
using GVisionWpf.Types;
using GVisionWpf.Visions.Engines;
using HalconDotNet;

namespace GVisionWpf.UIs.UiUpdaters
{
    public class LivePickerProcessor : ILiveFrameProcessor
    {
        private int index = 0;
        private readonly PadPitchCalibrationRepository padPitchRepository = PadPitchCalibrationRepository.Instance;

        public LivePickerProcessor(ECamera cameraType, HSmartWindowControlWPF hSmartWindowControlWpf)
        {
            this.CameraType = cameraType;
            this.HSmartWindowControlWpf = hSmartWindowControlWpf;
        }

        private CalibrationResult calculateOffset(HObject image)
        {
            PadPitchCalibrationTeaching teaching = this.padPitchRepository.GetRecipe();
            CalibrationResult result = CalibrationEngine.CalibrationEngineBase(image, teaching.Roi, teaching.Threshold, teaching.MinSize, teaching.MaxSize, teaching.Similarity, teaching.ShapeType, teaching.StandardType, ECalibration.PadPitch, this.CameraType);

            return result;
        }

        public override void Display(HObject image)
        {
            if (this.index++ % 3 != 0)
            {
                return;
            }

            CalibrationResult result = calculateOffset(image);

            this.HSmartWindowControlWpf.HalconWindow.DispObj(image);

            this.HSmartWindowControlWpf.HalconWindow.SetDraw("margin");
            this.HSmartWindowControlWpf.HalconWindow.SetColor("green");
            this.HSmartWindowControlWpf.HalconWindow.DispObj(result.Region);
            result.Region?.Dispose();

            displayText(result.IsFound ? result.Offset.ToString() : "NOT FOUND", 5, 5, color: (result.IsFound ? "green" : "red"));
        }

        public override void SetCameraType(ECamera type)
        {
            this.CameraType = type;
        }

        private void displayText(string text, int row, int col, string color = "green", ECoordinateSystem coordinate = ECoordinateSystem.Image, string font = "default-20", bool box = false, string boxColor = "green")
        {
            this.HSmartWindowControlWpf.HalconWindow.SetFont(font);
            this.HSmartWindowControlWpf.HalconWindow.SetColor(color);
            this.HSmartWindowControlWpf.HalconWindow.DispText(text, coordinate.ToString().ToLower(), row, col, color, new HTuple("box", "box_color"), new HTuple(box.ToString().ToLower(), boxColor));
        }
    }
}