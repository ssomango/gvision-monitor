using GVisionWpf.Models.Dtos.Common;

namespace GVisionWpf.Models.Dtos.Request
{
    public class StripBarcodeRequest
    {
        public CommonBody? CommonBody;
        public uint TriggerType;
        public uint CaptureDone;
        public StripBody? StripBody;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"TriggerType = {this.TriggerType}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"StripBody = {this.StripBody}";
        }
    }
}
