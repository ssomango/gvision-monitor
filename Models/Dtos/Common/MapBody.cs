using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Common
{
    [StructLayout(LayoutKind.Sequential)]
    public class MapBody : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint StripBarcode;

        [MarshalAs(UnmanagedType.U4)]
        public uint Sequence;

        [MarshalAs(UnmanagedType.U4)]
        public uint GridTableNum;

        [MarshalAs(UnmanagedType.U4)]
        public uint XPosition;

        [MarshalAs(UnmanagedType.U4)]
        public uint YPosition;

        public override string ToString()
        {
            return $"( StripBarcode : {this.StripBarcode}  " +
                   $"Sequence : {this.Sequence}  " +
                   $"GridTableNum : {this.GridTableNum}  " +
                   $"XPosition : {this.XPosition}  " +
                   $"YPosition : {this.YPosition} )";
        }
    }
}
