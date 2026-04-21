using GVisionWpf.Models.Dtos;
using GVisionWpf.Models.Dtos.Common;
using System.Runtime.InteropServices;

namespace GVisionWpf.DSMMI.Dto
{
    [StructLayout(LayoutKind.Sequential)]
    public class RecipeRequest : IBytesConvertible
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CommonBody? CommonBody;
    }
}
