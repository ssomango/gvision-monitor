using System.ComponentModel;

namespace GVisionWpf.Types
{
    public enum ESaveOption
    {
        [Description("No Save")]
        NoSave,
        [Description("Fail Only(W/O XOut)")]
        FailWithoutXOut,
        [Description("Fail Only")]
        Fail,
        [Description("Good & Fail")]
        GoodAndFail
    }
}
