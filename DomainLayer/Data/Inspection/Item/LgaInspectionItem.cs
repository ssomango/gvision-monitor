using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.DomainLayer.Data.Inspection.Item.FirstPin;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Lead;
using GVisionWpf.DomainLayer.Data.Inspection.Item.MultiPad;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Package;
using GVisionWpf.DomainLayer.Data.Inspection.Item.RejectMark;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Sawing;
using GVisionWpf.DomainLayer.Data.Inspection.Item.Surface;

namespace GVisionWpf.DomainLayer.Data.Inspection.Item
{
    public sealed partial class LgaInspectionItem : InspectionItem
    {
        public static readonly LgaInspectionItem XOut = new(nameof(XOut));

        public LgaInspectionItem(string name) : base(name) { }

        public override bool Equals(object? obj)
        {
            if (obj is not IInspectionItem other)
                return false;

            return Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(Name);

        private static readonly Dictionary<string, LgaInspectionItem> instances = new()
        {
            { nameof(XOut), XOut },

            { nameof(NoDevice), NoDevice },
            { nameof(PackageOffset), PackageOffset },
            { nameof(PackageSize), PackageSize },

            { nameof(RejectMark), RejectMark },

            { nameof(FirstPin), FirstPin },

            { nameof(MultiPadCount), MultiPadCount },
            { nameof(MultiPadSize), MultiPadSize },
            { nameof(MultiPadArea), MultiPadArea },
            { nameof(MultiPadPitch), MultiPadPitch },
            { nameof(MultiPadOffset), MultiPadOffset },
            { nameof(MultiPadContamination), MultiPadContamination },
            { nameof(MultiPadPerimeter), MultiPadPerimeter },

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

            { nameof(CornerDegree), CornerDegree },
            { nameof(SawOffset), SawOffset },
            { nameof(Chipping), Chipping },
            { nameof(Burr), Burr }
        };

        public static InspectionItem FromName(string name)
        {
            if (instances.TryGetValue(name, out var item))
                return item;

            throw new KeyNotFoundException($"LgaInspectionItem with name '{name}' does not exist.");
        }
    }

    public partial class LgaInspectionItem : IPackageInspectionItem<LgaInspectionItem>
    {
        #region Package Item
        public static LgaInspectionItem NoDevice { get; private set; } = new(nameof(NoDevice));
        public static LgaInspectionItem PackageOffset { get; private set; } = new(nameof(PackageOffset));
        public static LgaInspectionItem PackageSize { get; private set; } = new(nameof(PackageSize));
        #endregion
    }

    public partial class LgaInspectionItem : IRejectMarkInspectionItem<LgaInspectionItem>
    {
        #region Reject Mark Item
        public static LgaInspectionItem RejectMark { get; private set; } = new(nameof(RejectMark));
        #endregion
    }

    public partial class LgaInspectionItem : IFirstPinInspectionItem<LgaInspectionItem>
    {
        #region First Pin Item
        public static LgaInspectionItem FirstPin { get; private set; } = new(nameof(FirstPin));
        #endregion
    }

    public partial class LgaInspectionItem : IMultiPadInspectionItem<LgaInspectionItem>
    {
        #region MultiPad Item
        public static LgaInspectionItem MultiPadCount { get; private set; } = new(nameof(MultiPadCount));

        public static LgaInspectionItem MultiPadSize { get; private set; } = new(nameof(MultiPadSize));

        public static LgaInspectionItem MultiPadArea { get; private set; } = new(nameof(MultiPadArea));

        public static LgaInspectionItem MultiPadPitch { get; private set; } = new(nameof(MultiPadPitch));

        public static LgaInspectionItem MultiPadOffset { get; private set; } = new(nameof(MultiPadOffset));

        public static LgaInspectionItem MultiPadContamination { get; private set; } = new(nameof(MultiPadContamination));

        public static LgaInspectionItem MultiPadPerimeter { get; private set; } = new(nameof(MultiPadPerimeter));
        #endregion
    }

    public partial class LgaInspectionItem : ILeadInspectionItem<LgaInspectionItem>
    {
        #region Lead Item
        public static LgaInspectionItem LeadCount { get; private set; } = new(nameof(LeadCount));
        public static LgaInspectionItem LeadSize { get; private set; } = new(nameof(LeadSize));
        public static LgaInspectionItem LeadArea { get; private set; } = new(nameof(LeadArea));
        public static LgaInspectionItem LeadPitch { get; private set; } = new(nameof(LeadPitch));
        public static LgaInspectionItem LeadOffset { get; private set; } = new(nameof(LeadOffset));
        public static LgaInspectionItem LeadContamination { get; private set; } = new(nameof(LeadContamination));
        public static LgaInspectionItem LeadPerimeter { get; private set; } = new(nameof(LeadPerimeter));
        #endregion
    }

    public partial class LgaInspectionItem : ISurfaceInspectionItem<LgaInspectionItem>
    {
        #region Surface Item
        public static LgaInspectionItem Scratch { get; private set; } = new(nameof(Scratch));

        public static LgaInspectionItem ForeignMaterial { get; private set; } = new(nameof(ForeignMaterial));

        public static LgaInspectionItem Contamination { get; private set; } = new(nameof(Contamination));
        #endregion
    }

    public partial class LgaInspectionItem : ISawingInspectionItem<LgaInspectionItem>
    {
        #region Sawing Item
        public static LgaInspectionItem CornerDegree { get; private set; } = new(nameof(CornerDegree));
        public static LgaInspectionItem SawOffset { get; private set; } = new(nameof(SawOffset));
        public static LgaInspectionItem Chipping { get; private set; } = new(nameof(Chipping));
        public static LgaInspectionItem Burr { get; private set; } = new(nameof(Burr));
        #endregion
    }
}

