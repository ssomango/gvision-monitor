namespace GVisionWpf.DomainLayer.Extensions
{
    public static class PointExtensions
    {
        public static HObject GenReticle(this Point point, double T = 45, double size = 30)
        {
            HOperatorSet.GenCrossContourXld(out HObject reticle, point.Row, point.Col, size, new HTuple(T).TupleRad());
            return reticle;
        }
    }
}
