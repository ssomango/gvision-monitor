namespace GVisionWpf.Exceptions
{
    public class GFileNotFoundException : GVisionException
    {
        public GFileNotFoundException() : base("The file could not be found.")
        {
            ErrorCode = "FILE_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                "Please verify the path and ensure that the correct file is present.",
            };
        }

        public GFileNotFoundException(string message) : base(message) { }

        public GFileNotFoundException(string folderPath, string fileName) : base($"The file {fileName} could not be found.")
        {
            ErrorCode = "FILE_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                $"Please verify the path({folderPath}) and ensure that the correct file({fileName}) is present.",
            };
        }
    }
}