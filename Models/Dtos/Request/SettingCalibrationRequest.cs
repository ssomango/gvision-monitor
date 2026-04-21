using GVisionWpf.Models.Dtos.Common;

namespace GVisionWpf.Models.Dtos.Request
{
    public class SettingCalibrationRequest
    {
        public CommonBody? CommonBody;
        public uint Idk;
        public uint CaptureDone;
        public uint X1orX2;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"Idk = {this.Idk}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"X1orX2 = {this.X1orX2}";
        }
    }
}
