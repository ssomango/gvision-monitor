using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.PresentationLayer.Controllers;
using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Response
{
    [StructLayout(LayoutKind.Sequential)]
    public class StripBarcodeResponse : IBytesConvertible, IHasXYOffset
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody? CommonBody;

        [MarshalAs(UnmanagedType.U4)]
        public uint InspectionResult;

        [MarshalAs(UnmanagedType.U4)]
        public uint ErrorType;

        [field: MarshalAs(UnmanagedType.I4)]
        public int XOffset { get; set; }

        [field: MarshalAs(UnmanagedType.I4)]
        public int YOffset { get; set; }

        [MarshalAs(UnmanagedType.Struct)]
        public StripBody? StripBody;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"InspectionResult = {this.InspectionResult}  " +
                   $"ErrorType = {this.ErrorType}  " +
                   $"XOffset = {this.XOffset}  " +
                   $"YOffset = {this.YOffset}  " +
                   $"StripBody = {this.StripBody}";
        }
    }
}
