namespace GVisionWpf.Exceptions
{
    public class HalconLicenseException : GVisionException
    {
        public HalconLicenseException() : base("Halcon 라이선스가 없습니다.")
        {
            ErrorCode = "NO_HALCON_LICENSE";
            TroubleShooting = new List<string>
            {
                "Halcon 동글을 꽂으세요."
            };
        }
    }
}