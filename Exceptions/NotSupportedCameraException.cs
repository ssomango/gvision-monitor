namespace GVisionWpf.Exceptions
{
    public class NotSupportedCameraException : GVisionException
    {
        public NotSupportedCameraException() : base("지원하지 않는 카메라입니다.")
        {
            ErrorCode = "NOT_SUPPORTED_CAM";
            TroubleShooting = new List<string>
            {
                "설정한 카메라 정보가 올바른지 확인하세요.",
                "카메라 연결 상태를 확인하세요.",
            };
        }
    }
}