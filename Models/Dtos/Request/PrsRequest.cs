using GVisionWpf.Models.Dtos.Common;
using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Request
{
    [StructLayout(LayoutKind.Sequential)]
    public class PrsRequest : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 TriggerType;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 CaptureDone;

        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody? CommonBody;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public List<EachPrsBody> PrsBodies = new List<EachPrsBody>();

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();

            // Convert CommonBody to bytes and add to the list
            bytes.AddRange(((IBytesConvertible)this.CommonBody!).ToBytes());

            // Convert TriggerType to bytes and add to the list
            bytes.AddRange(BitConverter.GetBytes(this.TriggerType));

            // Convert CaptureDone to bytes and add to the list
            bytes.AddRange(BitConverter.GetBytes(this.CaptureDone));

            // Convert each EachPrsBody in PrsBodies to bytes and add to the list
            foreach (var prsBody in this.PrsBodies)
            {
                bytes.AddRange(((IBytesConvertible)prsBody!).ToBytes());
            }

            // If there are fewer than 8 EachPrsBody instances, add padding
            for (int i = this.PrsBodies.Count; i < 8; i++)
            {
                bytes.AddRange(new byte[32]);
            }

            return bytes.ToArray();
        }

        public override string ToString()
        {
            return $"CommonBody = {this.CommonBody}  " +
                   $"TriggerType = {this.TriggerType}  " +
                   $"CaptureDone = {this.CaptureDone}  " +
                   $"PrsBodies = {string.Join(",  ", this.PrsBodies)}";
        }
    }
}