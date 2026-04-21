using GVisionWpf.Models.Dtos.Common;

namespace GVisionWpf.Models.Dtos.Request
{
    public class ThreePointCalibrationRequest
    {
        public CommonBody? CommonBody;
        public uint TriggerType;
        public uint CaptureDone;
        public uint X1orX2;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"TriggerType = {this.TriggerType}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"X1orX2 = {this.X1orX2}";
        }
    }
}