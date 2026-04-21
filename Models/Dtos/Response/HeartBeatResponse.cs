using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Response
{
    [StructLayout(LayoutKind.Sequential)]
    public class HeartBeatResponse : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint Prefix = 0xffffffff;

        [MarshalAs(UnmanagedType.U4)]
        public uint DataLength = 0x18;

        [MarshalAs(UnmanagedType.U4)]
        public uint CommonHeader = 0x00;

        [MarshalAs(UnmanagedType.U4)]
        public uint CurrentVisionStatus = 0x1f;

        [MarshalAs(UnmanagedType.U4)]
        public uint CurrentVisionMode = 0x00;

        [MarshalAs(UnmanagedType.U4)]
        public uint PreVisionStatus = 0x00;
    }
}
