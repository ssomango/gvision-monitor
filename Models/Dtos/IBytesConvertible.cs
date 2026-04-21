using System.Runtime.InteropServices;

namespace GVisionWpf.Models.Dtos
{
    public interface IBytesConvertible
    {
        public byte[] ToBytes()
        {
            // 구조체에 할당된 메모리의 크기 저장
            int dataSize = Marshal.SizeOf(this);

            // 비관리 메모리 영역에 구조체 크기 만큼의 메모리 할당
            IntPtr buffer = Marshal.AllocHGlobal(dataSize);

            // 할당된 구조체 객체의 주소 저장
            Marshal.StructureToPtr(this, buffer, false);

            // 구조체가 복사될 배열
            byte[] bytes = new byte[dataSize];

            // 구조체 객체를 배열에 복사
            Marshal.Copy(buffer, bytes, 0, dataSize);

            // 비관리 메모리 영역에 할당한 메모리 해제
            Marshal.FreeHGlobal(buffer);

            return bytes;
        }
    }
}
