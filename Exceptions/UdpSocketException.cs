namespace GVisionWpf.Exceptions
{
    public class UdpSocketException : GVisionException
    {
        public UdpSocketException() : base("Udp Socket오류가 발생하였습니다.")
        {
            ErrorCode = "UDP_SOCK";
            TroubleShooting = new List<string>
            {
                "의도적으로 Disconnect한 경우, 오류가 아닐 수 있습니다.",
                "UdpClient가 유효한지 확인하세요.",
            };
        }
    }
}