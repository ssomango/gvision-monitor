using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Lead;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Pad;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Surface;

namespace GVisionWpf.DomainLayer.Data.Inspection.Item
{
    public partial class QfnInspectionItem : InspectionItem
    {
        public static readonly QfnInspectionItem XOut = new(nameof(XOut));

        public QfnInspectionItem(string name) : base(name) { }
        public override bool Equals(object? obj)
        {
            if (obj is not IInspectionItem other)
                return false;

            return Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(Name);

        private static readonly Dictionary<string, QfnInspectionItem> instances = new()
        {
            { nameof(XOut), XOut },

            { nameof(NoDevice), NoDevice },
            { nameof(PackageOffset), PackageOffset },
            { nameof(PackageSize), PackageSize },

            { nameof(CornerDegree), CornerDegree },
            { nameof(SawOffset), SawOffset },
            { nameof(Chipping), Chipping },
            { nameof(Burr), Burr },

            { nameof(FirstPin), FirstPin },

            { nameof(PadSize), PadSize },
            { nameof(PadArea), PadArea },

            { nameof(LeadCount), LeadCount },
            { nameof(LeadSize), LeadSize },
            { nameof(LeadArea), LeadArea },
            { nameof(LeadPitch), LeadPitch },
            { nameof(LeadOffset), LeadOffset },
            { nameof(LeadContamination), LeadContamination },
            { nameof(LeadPerimeter), LeadPerimeter },

            { nameof(Scratch), Scratch },
            { nameof(ForeignMaterial), ForeignMaterial },
            { nameof(Contamination), Contamination },

            { nameof(RejectMark), RejectMark }
        };

        public static InspectionItem FromName(string name)
        {
            if (instances.TryGetValue(name, out var item))
                return item;

            throw new KeyNotFoundException($"QfnInspectionItem with name '{name}' does not exist.");
        }
    }

    partial class QfnInspectionItem : IPackageInspectionItem<QfnInspectionItem>
    {
        #region Package Item
        public static QfnInspectionItem NoDevice { get; private set; } = new(nameof(NoDevice));
        public static QfnInspectionItem PackageOffset { get; private set; } = new(nameof(PackageOffset));
        public static QfnInspectionItem PackageSize { get; private set; } = new(nameof(PackageSize));
        #endregion
    }

    partial class QfnInspectionItem : ISawingInspectionItem<QfnInspectionItem>
    {
        #region Sawing Item
        public static QfnInspectionItem CornerDegree { get; private set; } = new(nameof(CornerDegree));

        public static QfnInspectionItem SawOffset { get; private set; } = new(nameof(SawOffset));

        public static QfnInspectionItem Chipping { get; private set; } = new(nameof(Chipping));

        public static QfnInspectionItem Burr { get; private set; } = new(nameof(Burr));
        #endregion
    }

    partial class QfnInspectionItem : IFirstPinInspectionItem<QfnInspectionItem>
    {
        #region First Pin Item
        public static QfnInspectionItem FirstPin { get; private set; } = new(nameof(FirstPin));
        #endregion
    }

    partial class QfnInspectionItem : ISinglePadInspectionItem<QfnInspectionItem>
    {
        #region SinglePad
        public static QfnInspectionItem PadSize { get; private set; } = new(nameof(PadSize));

        public static QfnInspectionItem PadArea { get; private set; } = new(nameof(PadArea));
        #endregion
    }

    partial class QfnInspectionItem : ILeadInspectionItem<QfnInspectionItem>
    {
        #region Lead Item
        public static QfnInspectionItem LeadCount { get; private set; } = new(nameof(LeadCount));

        public static QfnInspectionItem LeadSize { get; private set; } = new(nameof(LeadSize));

        public static QfnInspectionItem LeadArea { get; private set; } = new(nameof(LeadArea));

        public static QfnInspectionItem LeadPitch { get; private set; } = new(nameof(LeadPitch));

        public static QfnInspectionItem LeadOffset { get; private set; } = new(nameof(LeadOffset));

        public static QfnInspectionItem LeadContamination { get; private set; } = new(nameof(LeadContamination));

        public static QfnInspectionItem LeadPerimeter { get; private set; } = new(nameof(LeadPerimeter));
        #endregion
    }

    partial class QfnInspectionItem : ISurfaceInspectionItem<QfnInspectionItem>
    {
        #region Surface Item
        public static QfnInspectionItem Scratch { get; private set; } = new(nameof(Scratch));

        public static QfnInspectionItem ForeignMaterial { get; private set; } = new(nameof(ForeignMaterial));

        public static QfnInspectionItem Contamination { get; private set; } = new(nameof(Contamination));
        #endregion
    }

    partial class QfnInspectionItem : IRejectMarkInspectionItem<QfnInspectionItem>
    {
        #region Reject Mark Item
        public static QfnInspectionItem RejectMark { get; private set; } = new(nameof(RejectMark));
        #endregion
    }
}
