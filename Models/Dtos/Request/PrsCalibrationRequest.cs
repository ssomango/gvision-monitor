using GVisionWpf.Models.Dtos.Common;

namespace GVisionWpf.Models.Dtos.Request
{
    public class PrsCalibrationRequest
    {
        public uint InspectionResult;
        public uint ErrorType;
        public CommonBody? CommonBody;
        public List<EachPrsBody>? PrsBodies;

        public override string ToString()
        {
            string result = $"InspectionResult = {this.InspectionResult}  " +
                            $"ErrorType = {this.ErrorType}  " +
                            $"CommonBody = {this.CommonBody}  ";

            if (this.PrsBodies != null)
            {
                result += $"PrsBodies = {string.Join(",  ", this.PrsBodies)}";
            }

            return result;
        }
    }
}
