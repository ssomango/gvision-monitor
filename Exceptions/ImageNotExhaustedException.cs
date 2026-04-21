namespace GVisionWpf.Exceptions
{
    public class ImageNotExhaustedException : GVisionException
    {
        public ImageNotExhaustedException() : base("큐에서 이미지가 모두 소진되지 않았습니다.")
        {
            ErrorCode = "IMAGE_NOT_EXHAUSTED";
            TroubleShooting = new List<string>
            {
                "이미지가 피커 개수만큼 촬영되지 않았습니다.",
                "카메라 세팅을 확인해주세요.",
                "트리거가 제대로 발생하는지 확인해주세요.",
                "하드웨어 트리거모드인지 확인하세요."
            };
        }
    }
}