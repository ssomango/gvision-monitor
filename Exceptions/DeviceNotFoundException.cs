namespace GVisionWpf.Exceptions
{
    public class DeviceNotFoundException : VisionNotFoundException
    {
        public DeviceNotFoundException() : base("패키지를 찾지 못했습니다.")
        {
            ErrorCode = "DEVICE_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                "Edge Amplitude를 적절히 조절해주세요.",
                "Edge Detection 방향을 확인해주세요.",
                "조명 상태를 확인해주세요.",
                "디바이스가 실제로 있는지 확인해주세요."
            };
        }
    }
}