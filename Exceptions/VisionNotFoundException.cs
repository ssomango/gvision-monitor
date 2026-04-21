namespace GVisionWpf.Exceptions
{
    public class VisionNotFoundException : GVisionException
    {
        public VisionNotFoundException() : base("물체를 찾지 못했습니다.")
        {
            ErrorCode = "VISION_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                "Threshold를 적절히 조절해주세요.",
                "ROI 영역을 확인해주세요.",
            };
        }

        public VisionNotFoundException(string message) : base(message) { }
    }
}