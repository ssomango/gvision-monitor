namespace GVisionWpf.Exceptions
{
    public class NoIlluminationException : GVisionException
    {
        public NoIlluminationException() : base("Any illumination for this camera could not be found.")
        {
            ErrorCode = "NO_ILLUMINATION_ERROR";
            TroubleShooting = new List<string>
            {
                "Make sure illuminations for this camera are installed on the machine properly.",
                "Please check if the illumination recipe settings are correct."
            };
        }
    }
}
