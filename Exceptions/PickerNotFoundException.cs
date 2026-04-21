namespace GVisionWpf.Exceptions
{
    public class PickerNotFoundException : VisionNotFoundException
    {
        public PickerNotFoundException() : base("Picker를 찾지 못했습니다.")
        {
            ErrorCode = "PICKER_NOT_FOUND";
            TroubleShooting = new List<string>
            {
                "Threshold를 적절히 조절해주세요.",
                "ROI 영역을 확인해주세요.",
            };
        }
    }
}