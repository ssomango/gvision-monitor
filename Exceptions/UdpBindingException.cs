namespace GVisionWpf.Exceptions
{
    public class UdpBindingException : GVisionException
    {
        public UdpBindingException() : base("통신 연결에 실패했습니다.")
        {
            ErrorCode = "UDP_BIND_ERR";
            TroubleShooting = new List<string>
            {
                "Handler PC의 전원을 확인하세요.",
                "Handler 프로그램이 켜져있는지 확인하세요.",
                "GVision이 이미 실행 중인지 확인하세요."
            };
        }
    }
}