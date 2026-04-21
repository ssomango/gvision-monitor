namespace GVisionWpf.Exceptions
{
    public class NotInitializedCameraException : GVisionException
    {
        public NotInitializedCameraException() : base("지원하지 않는 카메라입니다.")
        {
            ErrorCode = "NOT_INITIALIZED_CAM";
            TroubleShooting = new List<string>
            {
                "카메라 연결 상태를 확인하세요.",
                "GVision을 재실행 하세요.",
            };
        }
    }
}