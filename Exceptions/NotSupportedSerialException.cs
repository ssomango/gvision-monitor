namespace GVisionWpf.Exceptions
{
    public class NotSupportedSerialException : GVisionException
    {
        public NotSupportedSerialException() : base("지원하지 않는 시리얼입니다.")
        {
            ErrorCode = "NOT_SUPPORTED_SERIAL";
            TroubleShooting = new List<string>
            {
                "COMPORT 번호가 올바른지 확인하세요.",
                "시리얼 케이블 연결 상태를 확인해 주세요.",
                "조명 컨트롤러가 연결되어 있는지 확인해 주세요."
            };
        }
    }
}