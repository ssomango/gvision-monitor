namespace GVisionWpf.Exceptions
{
    public class JigNotFoundException : VisionNotFoundException
    {
        public JigNotFoundException() : base("JIG를 찾지 못했습니다.")
        {
            ErrorCode = "JIG_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                "Threshold를 적절히 조절해주세요.",
                "ROI 영역을 확인해주세요.",
            };
        }
    }
}