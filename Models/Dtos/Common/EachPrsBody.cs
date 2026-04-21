using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Common;

[StructLayout(LayoutKind.Sequential)]
public class EachPrsBody : IBytesConvertible
{
    [MarshalAs(UnmanagedType.U4)]
    public uint StripBarcode;

    [MarshalAs(UnmanagedType.U4)]
    public uint Sequence;

    [MarshalAs(UnmanagedType.U4)]
    public uint GridTableNumber;

    [MarshalAs(UnmanagedType.U4)]
    public uint X1Orx2;

    [MarshalAs(UnmanagedType.U4)]
    public uint ZAxisNum;

    [MarshalAs(UnmanagedType.U4)]
    public uint HasDevice;

    [MarshalAs(UnmanagedType.U4)]
    public uint XPickPosition;

    [MarshalAs(UnmanagedType.U4)]
    public uint YPickPosition;

    public override string ToString()
    {
        return $"( StripBarcode : {this.StripBarcode}  " +
               $"Sequence : {this.Sequence}  " +
               $"GridTableNumber : {this.GridTableNumber}  " +
               $"X1Orx2 : {this.X1Orx2}  " +
               $"ZAxisNum : {this.ZAxisNum}  " +
               $"HasDevice : {this.HasDevice}  " +
               $"XPickPosition : {this.XPickPosition}  " +
               $"YPickPosition : {this.YPickPosition} )";
    }
}