using GVisionWpf.GlobalStates;

namespace GVisionWpf.Models.Visions
{
    public class SawOffset
    {
        public bool IsExistTargetObject;
        public double X, Y;

        public SawOffset()
        {
            this.X = 0;
            this.Y = 0;
            this.IsExistTargetObject = false;
        }

        public SawOffset(double x, double y)
        {
            this.X = x;
            this.Y = y;
            this.IsExistTargetObject = true;
        }

        public override string ToString()
        {
            if (!this.IsExistTargetObject)
            {
                return "Missing Object";
            }

            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            return $"X: {this.X.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}, " +
                   $"Y: {this.Y.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}";
        }
    }
}