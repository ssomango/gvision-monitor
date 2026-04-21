using GVisionWpf.Attributes;
using GVisionWpf.DomainLayer.Data.Inspection.Result.Package;

namespace GVisionWpf.Models.Entities.Result
{
    public abstract partial class InspectionResult : IDisposable
    {
        [IgnoreInToString]
        public DisposeBag DisposeBag { get; } = new DisposeBag();

        public int PackageNoInFov { get; set; }

        public HObject? Image;

        public List<HObject> Shots = new List<HObject>();

        public Result<bool> XOut { get; set; } = new Result<bool>();


        public EInspection Type;

        public int XPosition { get; set; }
        public int YPosition { get; set; }


        public DateTime StartTime { get; set; }
        public long Duration { get; set; }

        public EResultType? cachedErrorType;
        public string? cachedToString;

        public readonly object errorTypeLock = new object();
        public readonly object toStringLock = new object();

        public bool EvaluateResults() => ErrorType() == EResultType.Good;

        public EInspection InspectionType() => Type;

        public virtual EResultType ErrorType()
        {
            
            // X-Out is the highest priority.
            if (this.XOut.Type == EResultType.XOut)
            {
                return EResultType.XOut;
            }
            else if (this.XOut.Type == EResultType.XOut2)
            {
                return EResultType.XOut2;
            }


            if (this.cachedErrorType != null)
            {
                return this.cachedErrorType.Value;
            }

            lock (this.errorTypeLock)
            {
                this.cachedErrorType ??= InspectionResultConverter.ErrorTypeInEResultType(this);
            }

            return this.cachedErrorType.Value;
        }

        public override string ToString()
        {
            if (this.cachedToString != null)
            {
                return this.cachedToString;
            }

            lock (this.toStringLock)
            {
                this.cachedToString ??= InspectionResultConverter.ToString(this);
            }

            return this.cachedToString;
        }

        // INTENTION: 이미지와 RegionTransformer만 딥카피 합니다.
        // 나머지 값은 레퍼런스 타입으로 유지합니다. 값의 일관성 보장
        // 근데 리저트는 값이 변경되면 그것도 문제.
        public object Clone()
        {
            // Member-wise Shallow Copy
            var clonedResult = (InspectionResult)this.MemberwiseClone();

            // HObject 딥카피
            if (this.Image != null)
            {
                // INTENTION: 안쓰는 피커는 Shallow 카피해서 Dispose될 수 있도록 하려 했는데, 잘 안되서 일단 둘다 딥카피 하고,
                // PrsController에서 pickerInUse가 아닌애는 바로 Dispose하도록 수정하였음. 2025-01-02 YB
                if (this.ErrorType() == EResultType.NotInUsePicker)
                {
                    clonedResult.Image = this.Image.Clone();
                }
                else
                {
                    clonedResult.Image = this.Image.Clone();
                }

            }

            return clonedResult;
        }

        public void Dispose()
        {
            Image?.Dispose();
            Shots.ForEach(shot => shot.Dispose());

            PackageRegion?.Dispose();
            DisposeBag.Dispose();
        }
    }

    public partial class InspectionResult : IPackageInspectionResultModel<InspectionResult>
    {

        #region IPackageInspectionResult Properties
        public Result<bool> HasDevice { get; set; } = new Result<bool>();
        public Result<Pose> PackageOffset { get; set; } = new Result<Pose>();
        public Result<Size> PackageSize { get; set; } = new Result<Size>();
        
        public Result<SawOffset> SawOffset { get; set; } = new Result<SawOffset>();

        [IgnoreInToString]
        public HObject? PackageRegion { get; set; }

        [IgnoreInToString]
        public List<Point> PackagePoints { get; set; } = new List<Point>();
        #endregion
    }
}
