namespace GVisionWpf.Exceptions
{
    public class WrongGridSizeException : GVisionException
    {
        public WrongGridSizeException() : base("Wrong Grid Size")
        {
            ErrorCode = "WRONG_GRID";
            TroubleShooting = new List<string>
            {
                "Please check the grid size of mapping inspection is matching to the fov size."
            };
        }
    }
}
