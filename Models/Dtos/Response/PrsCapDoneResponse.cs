using System.Runtime.InteropServices;
using GVisionWpf.Models.Dtos.Common;

namespace GVisionWpf.Models.Dtos.Response
{
    [StructLayout(LayoutKind.Sequential)]
    public class CapDoneResponse : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody CommonBody = new CommonBody();

        [MarshalAs(UnmanagedType.U4)]
        public uint InspectionResult;

        [MarshalAs(UnmanagedType.U4)]
        public uint CaptureDone;

        [MarshalAs(UnmanagedType.U4)]
        public uint StripBarcode;

        [MarshalAs(UnmanagedType.U4)]
        public uint Sequence;

        [MarshalAs(UnmanagedType.U4)]
        public uint GridTableNumber;

        [MarshalAs(UnmanagedType.U4)]
        public uint XPickPosition;

        [MarshalAs(UnmanagedType.U4)]
        public uint YPickPosition;

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"InspectionResult = {this.InspectionResult}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"StripBarCode = {this.StripBarcode}  " +
                   $"Sequence = {this.Sequence}  " +
                   $"GridTableNumber = {this.GridTableNumber}  " +
                   $"XPickPosition = {this.XPickPosition}  " +
                   $"YPickPosition = {this.YPickPosition}  ";
        }
    }
}