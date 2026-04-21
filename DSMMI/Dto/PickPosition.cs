namespace GVisionWpf.DSMMI.Dto
{
    class PickPosition
    {
        public uint X;
        public uint Y;
        public uint Z;

        public PickPosition(uint x, uint y, uint z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }
}