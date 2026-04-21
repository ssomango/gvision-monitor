namespace GVisionWpf.Exceptions
{
    public class BlobNotFoundException : VisionNotFoundException
    {
        public BlobNotFoundException() : base("어떠한 Blob도 찾지 못했습니다.")
        {
            ErrorCode = "BLOB_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                "Threshold를 적절히 조절해주세요.",
                "blob으로 잡을 물체가 있는지 확인해주세요.",
                "조명 상태를 확인해주세요.",
            };
        }
    }
}