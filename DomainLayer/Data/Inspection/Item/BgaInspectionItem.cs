using GVisionWpf.DomainLayer.Data.Inspection.Item.Ball;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pattern;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Surface;

namespace GVisionWpf.DomainLayer.Data.Inspection.Item
{
    public sealed partial class BgaInspectionItem : InspectionItem
    {
        public static readonly BgaInspectionItem XOut = new(nameof(XOut));
        public static readonly BgaInspectionItem XOut2 = new(nameof(XOut2));

        public BgaInspectionItem(string name) : base(name) { }

        public override bool Equals(object? obj)
        {
            if (obj is not IInspectionItem other)
                return false;

            return Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(Name);

        private static readonly Dictionary<string, BgaInspectionItem> instances = new()
        {
            { nameof(XOut), XOut },
            { nameof(XOut2), XOut2 },

            { nameof(NoDevice), NoDevice },
            { nameof(PackageOffset), PackageOffset },
            { nameof(PackageSize), PackageSize },

            { nameof(FirstPin), FirstPin },

            { nameof(Pattern), Pattern },

            { nameof(BallCount), BallCount },
            { nameof(BallSize), BallSize },
            { nameof(BallPitch), BallPitch },
            { nameof(BallBridging), BallBridging },
            { nameof(ExtraBall), ExtraBall },
            { nameof(MissingBall), MissingBall },
            { nameof(CrackBall), CrackBall },
            { nameof(BallPosition), BallPosition },
            { nameof(BallLight), BallLight },

            { nameof(Scratch), Scratch },
            { nameof(ForeignMaterial), ForeignMaterial },
            { nameof(Contamination), Contamination },

            { nameof(CornerDegree), CornerDegree },
            { nameof(SawOffset), SawOffset },
            { nameof(Chipping), Chipping },
            { nameof(Burr), Burr },

            { nameof(RejectMark), RejectMark }
        };

        public static InspectionItem FromName(string name)
        {
            if (instances.TryGetValue(name, out var item))
                return item;

            throw new KeyNotFoundException($"BgaInspectionItem with name '{name}' does not exist.");
        }

    }

    partial class BgaInspectionItem : IPackageInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem NoDevice { get; private set; } = new(nameof(NoDevice));

        public static BgaInspectionItem PackageOffset { get; private set; } = new(nameof(PackageOffset));

        public static BgaInspectionItem PackageSize { get; private set; } = new(nameof(PackageSize));
    }

    partial class BgaInspectionItem : IFirstPinInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem FirstPin { get; private set; } = new(nameof(FirstPin));
    }

    partial class BgaInspectionItem : IPatternInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem Pattern { get; private set; } = new(nameof(Pattern));
    }

    partial class BgaInspectionItem : IBallInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem BallCount { get; private set; } = new(nameof(BallCount));

        public static BgaInspectionItem BallSize { get; private set; } = new(nameof(BallSize));

        public static BgaInspectionItem BallPitch { get; private set; } = new(nameof(BallPitch));

        public static BgaInspectionItem BallBridging { get; private set; } = new(nameof(BallBridging));

        public static BgaInspectionItem ExtraBall { get; private set; } = new(nameof(ExtraBall));

        public static BgaInspectionItem MissingBall { get; private set; } = new(nameof(MissingBall));

        public static BgaInspectionItem CrackBall { get; private set; } = new(nameof(CrackBall));

        public static BgaInspectionItem BallPosition { get; private set; } = new(nameof(BallPosition));

        public static BgaInspectionItem BallLight { get; private set; } = new(nameof(BallLight));
    }

    partial class BgaInspectionItem : ISurfaceInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem Scratch { get; private set; } = new(nameof(Scratch));

        public static BgaInspectionItem ForeignMaterial { get; private set; } = new(nameof(ForeignMaterial));

        public static BgaInspectionItem Contamination { get; private set; } = new(nameof(Contamination));
    }

    partial class BgaInspectionItem : ISawingInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem CornerDegree { get; private set; } = new(nameof(CornerDegree));

        public static BgaInspectionItem SawOffset { get; private set; } = new(nameof(SawOffset));

        public static BgaInspectionItem Chipping { get; private set; } = new(nameof(Chipping));

        public static BgaInspectionItem Burr { get; private set; } = new(nameof(Burr));
    }

    partial class BgaInspectionItem : IRejectMarkInspectionItem<BgaInspectionItem>
    {
        public static BgaInspectionItem RejectMark { get; private set; } = new(nameof(RejectMark));
    }
}
