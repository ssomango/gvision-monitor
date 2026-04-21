namespace GVisionWpf.Exceptions
{
    public class WrongValueException : GVisionException
    {
        public WrongValueException() : base("Wrong Parameter")
        {
            ErrorCode = "WRONG_PARAMETER";
            TroubleShooting = new List<string>
            {
                "Check your parameters."
            };
        }
    }
}