namespace GVisionWpf.Exceptions
{
    public class UdpSendException : GVisionException
    {
        public UdpSendException() : base("패킷 전송에 실패했습니다.")
        {
            ErrorCode = "UDP_SEND_ERR";
            TroubleShooting = new List<string> { };
        }
    }
}