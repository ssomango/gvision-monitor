using GVisionWpf.GlobalStates;
using System.Windows.Navigation;

namespace GVisionWpf.Models.Visions
{
    public partial struct Pose : IUnitConvertible<Pose>, IStatistical<Pose>
    {
        public double X, Y, T;

        public Pose(double x, double y, double t)
        {
            this.X = x;
            this.Y = y;
            this.T = t;
        }

        public Pose(Point point)
        {
            this.X = point.Col;
            this.Y = point.Row;
            this.T = 0;
        }

        public static Pose operator +(Pose p1, Pose p2)
        {
            return new Pose(p1.X + p2.X, p1.Y + p2.Y, p1.T + p2.T);
        }

        public static Pose operator -(Pose p1, Pose p2)
        {
            return new Pose(p1.X - p2.X, p1.Y - p2.Y, p1.T - p2.T);
        }

        public override string ToString()
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            return $"X: {this.X.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}, " +
                   $"Y: {this.Y.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}, " +
                   $"T: {this.T:N2}°";
        }

        public Pose ConvertFromPixel(ECamera cameraType)
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            Pose convertedPose = new Pose(
                x: unit.ConvertFromPixel(cameraType, this.X),
                y: unit.ConvertFromPixel(cameraType, this.Y),
                t: this.T
            );

            return convertedPose;
        }

        public Pose ConvertToPixel(ECamera cameraType)
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            Pose convertedPose = new Pose(
                x: unit.ConvertToPixel(cameraType, this.X),
                y: unit.ConvertToPixel(cameraType, this.Y),
                t: this.T
            );

            return convertedPose;
        }

        public Pose MemberWiseMin(List<Pose> list)
        {
            return new Pose(
                x: list.Min(p => p.X),
                y: list.Min(p => p.Y),
                t: list.Min(p => p.T)
            );
        }

        public Pose MemberWiseMax(List<Pose> list)
        {
            return new Pose(
                x: list.Max(p => p.X),
                y: list.Max(p => p.Y),
                t: list.Max(p => p.T)
            );
        }

        public Pose MemberWiseAverage(List<Pose> list)
        {
            return new Pose(
                x: list.Average(p => p.X),
                y: list.Average(p => p.Y),
                t: list.Average(p => p.T)
            );
        }
    }

    partial struct Pose
    {
        public bool IsZero() => X == 0 && Y == 0 && T == 0;
    }
}