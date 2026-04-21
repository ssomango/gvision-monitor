using GVisionWpf.Models.Dtos.Common;
using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Request
{
    [StructLayout(LayoutKind.Sequential)]
    public class MapRequest : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody? CommonBody;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 TriggerType;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 CaptureDone;

        [MarshalAs(UnmanagedType.Struct)]
        public MapBody? MapBody;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"TriggerType = {this.TriggerType}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"MapBody = {this.MapBody}";
        }
    }
}
