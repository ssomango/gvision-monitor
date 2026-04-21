namespace GVisionWpf.Exceptions
{
    public class NotSupportedCameraModeException : GVisionException
    {
        public NotSupportedCameraModeException() : base("지원하지 않는 카메라 모드입니다.")
        {
            ErrorCode = "NOT_SUPPORTED_CAM_MODE";
            TroubleShooting = new List<string>
            {
                "카메라가 설정한 모드(트리거)를 지원하는지 확인하세요.",
                "설정한 카메라 정보가 올바른지 확인하세요.",
                "카메라 연결 상태를 확인하세요.",
            };
        }
    }
}