namespace GVisionWpf.Exceptions
{
    public class DuplicatedLotNumberException : GVisionException
    {
        public DuplicatedLotNumberException() : base("This LOT number has already been taken.")
        {
            ErrorCode = "LOT_NUMBER_ERROR";
            TroubleShooting = new List<string>
            {
                "Please verify if the LOT Number is correct in the handler.",
                "You can ignore this exception, when you put the same LOT number multiple time in a row."
            };
        }
    }
}
