using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Request
{
    [StructLayout(LayoutKind.Sequential)]
    public class EachEmapBody : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint XPickPosition;

        [MarshalAs(UnmanagedType.U4)]
        public uint YPickPosition;

        [MarshalAs(UnmanagedType.U4)]
        public uint Data;

        [MarshalAs(UnmanagedType.U4)]
        public uint Dummy;

        public override string ToString()
        {
            return $"( XPickPosition : {this.XPickPosition}  " +
                   $"YPickPosition : {this.YPickPosition}  " +
                   $"Data : {this.Data}  " +
                   $"Dummy : {this.Dummy} )";
        }
    }
}
