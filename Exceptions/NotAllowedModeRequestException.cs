using System.Collections.Generic;

namespace GVisionWpf.Exceptions
{
    public class NotAllowedModeRequestException : GVisionException
    {
        public NotAllowedModeRequestException(string message) : base(message)
        {
            ErrorCode = "NOT_ALLOWED_MODE";
            TroubleShooting = new List<string>
            {
                "The requested feature is not supported in the current mode.",
                "Switch the mode.",
            };
        }
    }
}