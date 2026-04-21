namespace GVisionWpf.Utils
{
    // TODO: 설계 논의 필요
    public class Validation
    {
        // TODO: Dto.Tol ~~

        public static bool IsValidPackageOffset(Pose offset, bool isRotationAllowed = false)
        {
            const double tol = 5;
            bool result = (Math.Abs(offset.X) < tol) && (Math.Abs(offset.Y) < tol);

            if (isRotationAllowed)
            {
                result = result && (Math.Abs(offset.T - 90) < tol || Math.Abs(offset.T) < tol);
            }
            else
            {
                result = result && Math.Abs(offset.T) < tol;
            }

            return result;
        }

        public static bool IsValidPickerOffset(double x, double y, double t, bool isRotationAllowed = false)
        {
            const double tol = 0.5;
            bool result = (Math.Abs(x) < tol) && (Math.Abs(y) < tol);

            if (isRotationAllowed)
            {
                result = result && (Math.Abs(t - 90) < tol || Math.Abs(t) < tol);
            }
            else
            {
                result = result && Math.Abs(t) < tol;
            }

            return result;
        }
    }
}