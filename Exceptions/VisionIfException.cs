namespace GVisionWpf.Exceptions
{
    public class VisionIfException : GVisionException
    {
        public VisionIfException() : base("VISIONIF 파싱을 실패하였습니다.")
        {
            ErrorCode = "VISION_IF";
            TroubleShooting = new List<string> {
                "VisionIf 경로를 확인해주세요.",
                "VisionIf가 존재하는지 확인하세요.",
                "VisionIf 내용이 올바른지 확인하세요."
            };
        }
    }
}