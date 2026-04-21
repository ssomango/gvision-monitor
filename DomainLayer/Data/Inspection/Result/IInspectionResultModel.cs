namespace GVisionWpf.DomainLayer.Data.Inspection.Result
{
    public partial interface IInspectionResultModel : ICloneable, IDisposable
    {
        public DisposeBag DisposeBag { get; }
    }
}
