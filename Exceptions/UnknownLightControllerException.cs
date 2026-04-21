namespace GVisionWpf.Exceptions
{
    public class UnknownLightControllerException : GVisionException
    {
        public UnknownLightControllerException(string controllerName) : base($"알 수 없는 조명 컨트롤러({controllerName})를 사용하려고 시도하였습니다.")
        {
            ErrorCode = "UNKNOWN_LIGHT_CONTROLLER";
            TroubleShooting = new List<string>
            {
                "조명 설정 정보가 올바른지 확인하세요.",
                "조명 설정 정보와 조명 컨트롤러 정보가 일치하는지 확인하세요.",
                "조명 인터페이스 정보에 오타가 있는지 확인하세요."
            };
        }
    }
}