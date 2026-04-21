namespace GVisionWpf.Exceptions
{
    public class EmapPacketException : GVisionException
    {
        public EmapPacketException() : base("Wrong EMAP Packet has been received.")
        {
            ErrorCode = "WRONG_EMAP_PACKET";
            TroubleShooting = new List<string>
            {
                "Failed to parse the received EMAP Packet."
            };
        }
    }
}