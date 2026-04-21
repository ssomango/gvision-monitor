using System;
using System.Collections.Generic;
using System.Linq;
using GVisionWpf.GlobalStates;
using GVisionWpf.Types;
using GVisionWpf.Utils;

namespace GVisionWpf.Models.Visions
{
    public class Length : IUnitConvertible<Length>, IStatistical<Length>
    {
        public double Value;

        public Length()
        {
            this.Value = 0;
        }

        public Length(double value)
        {
            this.Value = value;
        }

        public static Length operator +(Length a, Length b)
        {
            return new Length(a.Value + b.Value);
        }

        public static Length operator *(Length a, double b)
        {
            return new Length(a.Value* b);
        }

        public static Length operator -(Length a, Length b)
        {
            return new Length(a.Value - b.Value);
        }

        public static Length operator *(Length a, Length b)
        {
            return new Length(a.Value * b.Value);
        }

        public static Length operator /(Length a, Length b)
        {
            if (b.Value == 0)
            {
                throw new DivideByZeroException();
            }

            return new Length(a.Value / b.Value);
        }

        public static Length operator /(Length a, int b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException();
            }

            return new Length(a.Value / b);
        }

        public static bool operator <(Length a, Length b)
        {
            return a.Value < b.Value;
        }

        public static bool operator >(Length a, Length b)
        {
            return a.Value > b.Value;
        }

        public Length Abs()
        {
            double value = this.Value;
            return new Length(value < 0 ? -value : value);
        }

        public override string ToString()
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            return $"{this.Value.ToString($"F{unit.DecimalPlaces}")}{unit.Symbol}";
        }

        public Length ConvertFromPixel(ECamera cameraType)
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            Length convertedLength = new Length(
                value: unit.ConvertFromPixel(cameraType, this.Value)
            );

            return convertedLength;
        }

        public Length ConvertToPixel(ECamera cameraType)
        {
            LengthUnit unit = GlobalSetting.Instance.Inspection.LengthUnit;

            Length convertedLength = new Length(
                value: unit.ConvertToPixel(cameraType, this.Value)
            );

            return convertedLength;
        }

        public Length MemberWiseMin(List<Length> list)
        {
            return new Length(list.Min(l => l.Value));
        }

        public Length MemberWiseMax(List<Length> list)
        {
            return new Length(list.Max(l => l.Value));
        }

        public Length MemberWiseAverage(List<Length> list)
        {
            return new Length(list.Average(l => l.Value));
        }
    }
}