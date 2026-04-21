using GVisionWpf.DomainLayer.Data;
using GVisionWpf.GlobalStates;

namespace GVisionWpf.Extensions
{
    public static class EResultTypeExtensions
    {
        public static EColor GetResultColor(this EResultType resultType, InspectionTeaching teaching)
        {
            if (teaching is MoldTeaching || teaching is GridMoldTeaching) return resultType == EResultType.Good ? EColor.Green : GlobalSetting.Instance.Inspection.MapColors[resultType];
            else if (teaching is BgaTeaching || teaching is GridBgaTeaching) return resultType == EResultType.Good ? EColor.Green : GlobalSetting.Instance.Inspection.BgaColors[resultType];
            else if (teaching is LgaTeaching || teaching is GridLgaTeaching) return resultType == EResultType.Good ? EColor.Green : GlobalSetting.Instance.Inspection.LgaColors[resultType];
            else if (teaching is QfnTeaching || teaching is GridQfnTeaching) return resultType == EResultType.Good ? EColor.Green : GlobalSetting.Instance.Inspection.QfnColors[resultType];
            else throw new Exception($"Unsupported inspection type: {teaching.GetType()}");
        }
    }
}
