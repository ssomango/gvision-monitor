using System.Runtime.Serialization;

namespace GVisionWpf.Models.Visions
{
    public class Tolerance
    {
        // BGA Tolerances
        public Size BgaPackageSize;
        public double BgaCornerDegree;
        public double BgaBallSizeDiameter;
        public double BgaBallPitch;
        public double BgaSawOffsetX;
        public double BgaSawOffsetY;
        public Point BgaBallPosition = null!;

        // QFN Tolerances
        public Size QfnPackageSize;
        public double QfnCornerDegree;
        public double QfnPadSizeWidth;
        public double QfnPadSizeHeight;
        public double QfnSawOffsetX;
        public double QfnSawOffsetY;
        public int QfnPadArea;
        public Size QfnLeadSize;
        public double QfnLeadPitch;
        public int QfnLeadArea;
        public Pose QfnLeadOffset;
        public double QfnLeadPerimeter;

        // MAP Tolerances
        public Size MapPackageSize;
        public int MarkCount;
        public double MapTextOffsetX;
        public double MapTextOffsetY;
        public double MapTextOffsetT;
        public double MapCornerDegree;
        public double MapSawOffsetX;
        public double MapSawOffsetY;

        // LGA Tolerances
        public Size LgaPackageSize;
        public double LgaCornerDegree;
        public double LgaSawOffsetX;
        public double LgaSawOffsetY;
        public Size LgaPadSize;
        public double LgaPadPitch;
        public int LgaPadArea;
        public Pose LgaPadOffset;
        public double LgaPadPerimeter;
        public Size LgaLeadSize;
        public double LgaLeadPitch;
        public int LgaLeadArea;
        public Pose LgaLeadOffset;
        public double LgaLeadPerimeter;

        [OnDeserialized] 
        internal void OnDeserializedMethod(StreamingContext context)
        {
            this.BgaBallPosition ??= new Point(10, 10);
        }
    }
}