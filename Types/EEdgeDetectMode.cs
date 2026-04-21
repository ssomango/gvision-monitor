using System.ComponentModel;

namespace GVisionWpf.Types
{
    public enum EEdgeDetectMode
    {
        [Description("Black → White")]
        BlackToWhite,
        [Description("White → Black")]
        WhiteToBlack,
    }
}