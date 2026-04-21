namespace GVisionWpf.Exceptions
{
    public class CameraTriggerException : GVisionException
    {
        public CameraTriggerException() : base("장비에서 카메라 트리거가 발생하지 않았습니다.")
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