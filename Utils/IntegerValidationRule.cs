using System.Globalization;
using System.Windows.Controls;

namespace GVisionWpf.Utils
{
    public class IntegerRangeRule : ValidationRule
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int number = 0;

            try
            {
                if (((string)value).Length > 0)
                    number = Int32.Parse((String)value);
            }

            catch (Exception e)
            {
                return new ValidationResult(false, $"Illegal characters or {e.Message}");
            }

            if ((number < Min) || (number > Max))
            {
                return new ValidationResult(false, $"Please enter in the range: {Min}-{Max}.");
            }

            return ValidationResult.ValidResult;
        }
    }
}