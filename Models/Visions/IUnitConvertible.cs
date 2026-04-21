namespace GVisionWpf.Models.Visions
{
    public interface IUnitConvertible<out T>
    {
        public T ConvertFromPixel(ECamera cameraType);
        public T ConvertToPixel(ECamera cameraType);
    }
}