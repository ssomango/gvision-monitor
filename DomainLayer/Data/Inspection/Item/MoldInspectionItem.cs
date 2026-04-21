using GVisionWpf.DomainLayer.Data.Inspection.Item.DataCode;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Mark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Surface;

namespace GVisionWpf.DomainLayer.Data.Inspection.Item
{
    public sealed partial class MoldInspectionItem : InspectionItem
    {
        public static readonly MoldInspectionItem XOut = new(nameof(XOut));
        public static readonly MoldInspectionItem XOut2 = new(nameof(XOut2));

        public MoldInspectionItem(string name) : base(name) { }

        public override bool Equals(object? obj)
        {
            if (obj is not IInspectionItem other)
                return false;

            return Name == other.Name;
        }

        public override string ToString() => Name;
        public override int GetHashCode() => HashCode.Combine(Name);

        private static readonly Dictionary<string, MoldInspectionItem> instances = new(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(XOut), XOut },
            { nameof(XOut2), XOut2 },
            { nameof(NoDevice), NoDevice },
            { nameof(PackageSize), PackageSize },
            { nameof(PackageOffset), PackageOffset },
            { nameof(DataCode), DataCode },
            { nameof(NoMark), NoMark },
            { nameof(TextAngle), TextAngle },
            { nameof(TextOffset), TextOffset },
            { nameof(MissingChar), MissingChar },
            { nameof(MarkCount), MarkCount },
            { nameof(WrongMark), WrongMark },
            { nameof(RejectMark), RejectMark },
            { nameof(Scratch), Scratch },
            { nameof(ForeignMaterial), ForeignMaterial },
            { nameof(Contamination), Contamination },
            { nameof(CornerDegree), CornerDegree },
            { nameof(Burr), Burr },
            { nameof(Chipping), Chipping },
            { nameof(SawOffset), SawOffset }
        };

        public static InspectionItem FromName(string name)
        {
            if (instances.TryGetValue(name, out var item))
                return item;

            throw new KeyNotFoundException($"MoldInspectionItem with name '{name}' does not exist.");
        }
    }

    partial class MoldInspectionItem : IPackageInspectionItem<MoldInspectionItem>
    {
        #region Package Item
        public static MoldInspectionItem NoDevice { get; private set; } = new(nameof(NoDevice));
        public static MoldInspectionItem PackageSize { get; private set; } = new(nameof(PackageSize));
        public static MoldInspectionItem PackageOffset { get; private set; } = new(nameof(PackageOffset));
        #endregion
    }

    partial class MoldInspectionItem : IDataCodeInspectionItem<MoldInspectionItem>
    {
        #region DataCode Item
        public static MoldInspectionItem DataCode { get; private set; } = new(nameof(DataCode));
        #endregion
    }

    partial class MoldInspectionItem : IMarkInspectionItem<MoldInspectionItem>
    {
        #region Mark Item
        public static MoldInspectionItem NoMark { get; private set; } = new(nameof(NoMark));
        public static MoldInspectionItem TextAngle { get; private set; } = new(nameof(TextAngle));
        public static MoldInspectionItem TextOffset { get; private set; } = new(nameof(TextOffset));
        public static MoldInspectionItem MissingChar { get; private set; } = new(nameof(MissingChar));
        public static MoldInspectionItem MarkCount { get; private set; } = new(nameof(MarkCount));
        public static MoldInspectionItem WrongMark { get; private set; } = new(nameof(WrongMark));
        #endregion
    }

    partial class MoldInspectionItem : IRejectMarkInspectionItem<MoldInspectionItem>
    {
        #region Reject Mark Item
        public static MoldInspectionItem RejectMark { get; private set; } = new(nameof(RejectMark));
        #endregion
    }

    partial class MoldInspectionItem : ISurfaceInspectionItem<MoldInspectionItem>
    {
        #region Surface Item
        public static MoldInspectionItem Scratch { get; private set; } = new(nameof(Scratch));
        public static MoldInspectionItem ForeignMaterial { get; private set; } = new(nameof(ForeignMaterial));
        public static MoldInspectionItem Contamination { get; private set; } = new(nameof(Contamination));
        #endregion
    }

    partial class MoldInspectionItem : ISawingInspectionItem<MoldInspectionItem>
    {
        #region Sawing Item
        public static MoldInspectionItem CornerDegree { get; private set; } = new(nameof(CornerDegree));
        public static MoldInspectionItem Burr { get; private set; } = new(nameof(Burr));
        public static MoldInspectionItem Chipping { get; private set; } = new(nameof(Chipping));
        public static MoldInspectionItem SawOffset { get; private set; } = new(nameof(SawOffset));
        #endregion region
    }
}