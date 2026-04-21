namespace GVisionWpf.Exceptions
{
    public class WrongSettingException : GVisionException
    {
        public WrongSettingException() : base("Wrong Setting")
        {
            ErrorCode = "WRONG_SETTING";
            TroubleShooting = new List<string>
            {
                "Please check if the teaching and recipe settings are correct."
            };
        }
    }
}
