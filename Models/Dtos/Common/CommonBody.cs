using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Common;

[StructLayout(LayoutKind.Sequential)]
public class CommonBody : IBytesConvertible
{
    [MarshalAs(UnmanagedType.U4)]
    public uint Prefix = 0xffffffff;

    [MarshalAs(UnmanagedType.U4)]
    public uint DataLength;

    [MarshalAs(UnmanagedType.U4)]
    public uint CommonHeader = 0x01;

    [MarshalAs(UnmanagedType.U4)]
    public uint CameraId;

    [MarshalAs(UnmanagedType.U4)]
    public uint InspectionType;

    public override string ToString()
    {
        return $"( Prefix : {this.Prefix}  " +
               $"DataLength : {this.DataLength}  " +
               $"CommonHeader : {this.CommonHeader}  " +
               $"CameraId : {this.CameraId}  " +
               $"InspectionType : {this.InspectionType} )";
    }
}