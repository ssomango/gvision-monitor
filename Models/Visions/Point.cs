using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace GVisionWpf.Models.Visions
{
    public sealed partial class Point : ObservableObject
    {
        [ObservableProperty]
        private double row;

        [ObservableProperty]
        private double col;

        [JsonConstructor]
        public Point(double row, double col)
        {
            Row = row;
            Col = col;
        }

        public Point(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString()
        {
            return $"Row: {Row}, Column: {Col}";
        }
    }

    public partial class Point
    {
        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.Row + p2.Row, p1.Col + p2.Col);
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.Row - p2.Row, p1.Col - p2.Col);
        }

        public static Point operator *(Point p, int value)
        {
            return new Point(p.Row * value, p.Col * value);
        }

        public static Point operator /(Point p, int value)
        {
            return new Point(p.Row / value, p.Col / value);
        }
    }
}