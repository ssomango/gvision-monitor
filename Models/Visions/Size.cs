using GVisionWpf.GlobalStates;

namespace GVisionWpf.Models.Visions
{
    public partial struct Size : IUnitConvertible<Size>, IStatistical<Size>
    {
        public double Width, Height;

        public Size(double width, double height)
        {
            this.Width = width;
            this.Height = height;
        }

        public override string ToString()
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            return $"Width: {this.Width.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}, " +
                   $"Height: {this.Height.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}";
        }

        public Size ConvertFromPixel(ECamera cameraType)
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            Size convertedSize = new Size(
                width: unit.ConvertFromPixel(cameraType, this.Width),
                height: unit.ConvertFromPixel(cameraType, this.Height)
            );

            return convertedSize;
        }

        public Size ConvertToPixel(ECamera cameraType)
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            Size convertedSize = new Size(
                width: unit.ConvertFromPixel(cameraType, this.Width),
                height: unit.ConvertFromPixel(cameraType, this.Height)
            );

            return convertedSize;
        }

        public Size MemberWiseMin(List<Size> list)
        {
            return new Size(
                width: list.Min(s => s.Width),
                height: list.Min(s => s.Height)
            );
        }

        public Size MemberWiseMax(List<Size> list)
        {
            return new Size(
                width: list.Max(s => s.Width),
                height: list.Max(s => s.Height)
            );
        }

        public Size MemberWiseAverage(List<Size> list)
        {
            return new Size(
                width: list.Average(s => s.Width),
                height: list.Average(s => s.Height)
            );
        }
    }

    partial struct Size
    {
        public bool IsZero() => Width == 0 && Height == 0;
    }
}