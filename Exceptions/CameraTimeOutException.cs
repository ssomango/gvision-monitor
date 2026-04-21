namespace GVisionWpf.Exceptions
{
    public class CameraTimeOutException : GVisionException
    {
        public CameraTimeOutException() : base("타임아웃내에 이미지를 획득하는데 실패했습니다.")
        {
            ErrorCode = "CAM_NOT_TRIGGERED";
            TroubleShooting = new List<string>
            {
                "핸들러에서 트리거 설정이 올바른지 확인하세요.",
                "핸들러에서 검사를 재요청해주세요.",
            };
        }
    }
}