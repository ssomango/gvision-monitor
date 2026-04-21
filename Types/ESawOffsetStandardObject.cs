using System.ComponentModel;

namespace GVisionWpf.Types
{
    public enum ESawOffsetStandardObject
    {
        [Description("BALL")]
        Ball = 0,

        [Description("PATTERN")]
        Pattern = 1,

        [Description("FIRST PIN")]
        FirstPin = 2,

        [Description("PAD")]
        Pad = 3,

        [Description("LEAD")]
        Lead = 4,

        [Description("MARK")]
        Mark = 5
    }
}