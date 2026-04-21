using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos.Common
{
    [StructLayout(LayoutKind.Sequential)]
    public class StripBody
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public char[] StripBarcode = new char[128];

        [MarshalAs(UnmanagedType.U4)]
        public uint StripCount;

        public override string ToString()
        {
            return $"( StripBarcode : {string.Join(" ", this.StripBarcode)}  " +
                   $"StripCount : {this.StripCount} )";
        }
    }
}
