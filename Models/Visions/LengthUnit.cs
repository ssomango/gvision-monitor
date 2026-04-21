using GVisionWpf.Cameras;

namespace GVisionWpf.Models.Visions
{
    public class LengthUnit
    {
        public readonly EMeasurementUnit UnitType;
        public readonly string Symbol;
        public readonly int DecimalPlaces;
        public readonly double RelativeWeight; // relative to mm.

        public LengthUnit(EMeasurementUnit unitType, string symbol, double relativeWeight, int decimalPlaces)
        {
            this.UnitType = unitType;
            this.Symbol = symbol;
            this.RelativeWeight = relativeWeight;
            this.DecimalPlaces = decimalPlaces;
        }

        public double ConvertFromPixel(ECamera cameraType, double pxValue)
        {
            Camera camera = CameraManager.Instance.Cameras![cameraType];
            double mmValue = pxValue / camera.PixelPerMillimeter;
            double convertedValue = mmValue * this.RelativeWeight;

            return convertedValue;
        }

        public double ConvertToPixel(ECamera cameraType, double value)
        {
            Camera camera = CameraManager.Instance.Cameras![cameraType];
            double mmValue = value / this.RelativeWeight;
            double pxValue = mmValue * camera.PixelPerMillimeter;

            return pxValue;
        }
    }
}