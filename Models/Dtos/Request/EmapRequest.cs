using GVisionWpf.Models.Dtos.Common;
using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Request
{
    [StructLayout(LayoutKind.Sequential)]
    public class EmapRequest : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody? CommonBody;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 TriggerType;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 CaptureDone;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 Sequence;

        [MarshalAs(UnmanagedType.U4)]
        public uint GridTableNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public List<EachEmapBody> EmapBodies = new List<EachEmapBody>();


        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(((IBytesConvertible)this.CommonBody!).ToBytes());
            bytes.AddRange(BitConverter.GetBytes(this.TriggerType));
            bytes.AddRange(BitConverter.GetBytes(this.CaptureDone));
            bytes.AddRange(BitConverter.GetBytes(this.Sequence));
            bytes.AddRange(BitConverter.GetBytes(this.GridTableNumber));

            foreach (var emapbody in this.EmapBodies)
            {
                bytes.AddRange(((IBytesConvertible)emapbody!).ToBytes());
            }

            return bytes.ToArray();
        }

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"TriggerType = {this.TriggerType}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"Sequence = {this.Sequence}  " +
                   $"GridTableNumber = {this.GridTableNumber}  " +
                   $"EmapBodies = {string.Join(",  ", this.EmapBodies)}";
        }
    }
}
