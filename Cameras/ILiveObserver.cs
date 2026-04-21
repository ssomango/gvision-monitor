
namespace GVisionWpf.Cameras
{
    public interface ILiveObserver
    {
        public void UpdateFrame(HObject image);
    }
}
