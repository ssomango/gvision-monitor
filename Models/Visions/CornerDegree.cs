namespace GVisionWpf.Models.Visions
{
    public partial struct CornerDegree
    {
        public double TopLeft, TopRight, BottomLeft, BottomRight;

        public CornerDegree()
        {
            this.TopLeft = this.TopRight = this.BottomLeft = this.BottomRight = 90;
        }

        public CornerDegree(double topLeft, double topRight, double bottomLeft, double bottomRight)
        {
            this.TopLeft = topLeft;
            this.TopRight = topRight;
            this.BottomLeft = bottomLeft;
            this.BottomRight = bottomRight;
        }

        public override string ToString()
        {
            return $"TL: {this.TopLeft:N2}° TB: {this.TopRight:N2}° BL: {this.BottomLeft:N2}° BR: {this.BottomRight:N2}°";
        }
    }

    public partial struct CornerDegree
    {
        public double this[int index]
        {
            get => index switch
            {
                0 => TopLeft,
                1 => TopRight,
                2 => BottomLeft,
                3 => BottomRight,
                _ => throw new IndexOutOfRangeException("Index must be between 0 and 3.")
            };
            set
            {
                switch (index)
                {
                    case 0: TopLeft = value; break;
                    case 1: TopRight = value; break;
                    case 2: BottomLeft = value; break;
                    case 3: BottomRight = value; break;
                    default: throw new IndexOutOfRangeException("Index must be between 0 and 3.");
                }
            }
        }
    }
}