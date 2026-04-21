namespace GVisionWpf.Types
{
    public enum EResultType
    {
        Good,
        NoDevice,
        NotInUsePicker,
        PackageOffset,
        PackageSize,
        Pattern,
        BallCount,
        CrackBall,
        TextAngle,
        TextOffset,
        DataCode,
        MissingChar,

        Scratch,
        SawOffset,
        CornerDegree,
        Contamination,
        ForeignMaterial,
        Chipping,
        Burr,

        RejectMark,
        FirstPin,
        PadSize,
        PadArea,
        LeadSize,
        LeadArea,
        LeadCount,
        LeadPitch,
        LeadOffset,
        LeadPerimeter,
        LeadContamination,
        NoMark,
        MarkCount,
        WrongMark,
        MissingBall,
        ExtraBall,
        BallSize,
        BallBridging,
        BallPosition,
        BallPitch,
        BallLight,

        MultiPadSize,
        MultiPadArea,
        MultiPadCount,
        MultiPadPitch,
        MultiPadOffset,
        MultiPadPerimeter,
        MultiPadContamination,

        XOut,
        XOut2
    }
}
