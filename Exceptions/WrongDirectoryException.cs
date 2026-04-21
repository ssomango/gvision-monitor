namespace GVisionWpf.Exceptions
{
    public class WrongDirectoryException : GVisionException
    {
        public WrongDirectoryException() : base("잘못된 디렉토리입니다.")
        {
            ErrorCode = "WRONG_DIR";
            TroubleShooting = new List<string>
            {
                "디렉토리명을 제대로 입력하세요."
            };
        }
    }
}