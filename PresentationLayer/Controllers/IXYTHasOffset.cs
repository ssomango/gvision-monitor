namespace GVisionWpf.PresentationLayer.Controllers
{
    public interface IHasXYTOffset : IHasXYOffset
    {
        int TOffset { get; set; }
    }
}