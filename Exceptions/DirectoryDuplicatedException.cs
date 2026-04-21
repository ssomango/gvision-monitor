namespace GVisionWpf.Exceptions
{
    public class DirectoryDuplicatedException : GVisionException
    {
        public DirectoryDuplicatedException() : base("해당 디렉터리가 존재합니다..")
        {
            ErrorCode = "DIR_DUPLICATED";
            TroubleShooting = new List<string>
            {
                "다른 폴더 이름을 사용하세요."
            };
        }
    }
}