namespace GVisionWpf.Exceptions
{
    public class InspectionNotSelectedException : GVisionException
    {
        public InspectionNotSelectedException() : base("검사 유형이 선택되지 않았습니다.")
        {
            ErrorCode = "INSPECTION_NOT_SELECTED";
            TroubleShooting = new List<string>
            {
                "디바이스 정보가 유효한지 확인해주세요.",
                "검사 유형이 설정되어 있는지 확인해주세요.",
            };
        }
    }
}