namespace GVisionWpf.Exceptions
{
    public class GVisionException : Exception
    {
        public string ErrorCode { get; set; }
        public List<string> TroubleShooting { get; set; }
        public string StackTrace { get; set; }

        public GVisionException()
        {
            ErrorCode = "UNKNOWN_ERROR_CODE";
            TroubleShooting = new List<string> { "Troubleshooting information not available." };
        }

        public GVisionException(Exception ex) : base(ex.Message, ex)
        {
            ErrorCode = "SYSTEM_ERROR";
            TroubleShooting = new List<string> { "Troubleshooting information not available." };
        }

        public GVisionException(string message) : base(message)
        {
            ErrorCode = "XERR_000";
            TroubleShooting = new List<string>();
        }

        public GVisionException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
            TroubleShooting = new List<string>();
        }

        public GVisionException(string message, string errorCode, List<string> troubleShooting) : base(message)
        {
            ErrorCode = errorCode;
            TroubleShooting = troubleShooting;
        }
    }
}