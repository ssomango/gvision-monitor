using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.PresentationLayer.Controllers;
using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Response
{
    [StructLayout(LayoutKind.Sequential)]
    public class PrsCalibrationResponse : IBytesConvertible, IHasXYTOffset
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody CommonBody = new CommonBody();

        [MarshalAs(UnmanagedType.U4)]
        public uint InspectionResult;

        [MarshalAs(UnmanagedType.U4)]
        public uint ErrorType;

        [field: MarshalAs(UnmanagedType.I4)]
        public int XOffset { get; set; }

        [field: MarshalAs(UnmanagedType.I4)]
        public int YOffset { get; set; }

        [field: MarshalAs(UnmanagedType.I4)]
        public int TOffset { get; set; }

        [MarshalAs(UnmanagedType.Struct)]
        public EachPrsBody? PrsBody;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"InspectionResult = {this.InspectionResult}  " +
                   $"ErrorType = {this.ErrorType}  " +
                   $"XOffset = {this.XOffset}  " +
                   $"YOffset = {this.YOffset}  " +
                   $"TOffset = {this.TOffset}  " +
                   $"PrsBody = {this.PrsBody}";
        }
    }
}
