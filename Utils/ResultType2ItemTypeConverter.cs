using GVisionWpf.DomainLayer.Data.Inspection.Item;

namespace GVisionWpf.Utils
{
    public class ResultType2ItemTypeConverter
    {
        public readonly Dictionary<EResultType, BgaInspectionItem> Bga = new Dictionary<EResultType, BgaInspectionItem>
        {
            { EResultType.NoDevice, BgaInspectionItem.NoDevice },
            { EResultType.PackageOffset, BgaInspectionItem.PackageOffset },
            { EResultType.PackageSize, BgaInspectionItem.PackageSize },
            { EResultType.CornerDegree, BgaInspectionItem.CornerDegree },
            { EResultType.FirstPin, BgaInspectionItem.FirstPin },
            { EResultType.Pattern, BgaInspectionItem.Pattern },
            { EResultType.BallCount, BgaInspectionItem.BallCount },
            { EResultType.BallSize, BgaInspectionItem.BallSize },
            { EResultType.BallPitch, BgaInspectionItem.BallPitch },
            { EResultType.BallBridging, BgaInspectionItem.BallBridging },
            { EResultType.ExtraBall, BgaInspectionItem.ExtraBall },
            { EResultType.MissingBall, BgaInspectionItem.MissingBall },
            { EResultType.CrackBall, BgaInspectionItem.CrackBall },
            { EResultType.Scratch, BgaInspectionItem.Scratch },
            { EResultType.ForeignMaterial, BgaInspectionItem.ForeignMaterial },
            { EResultType.Contamination, BgaInspectionItem.Contamination },
            { EResultType.BallPosition, BgaInspectionItem.BallPosition },
            { EResultType.SawOffset, BgaInspectionItem.SawOffset },
            { EResultType.Chipping, BgaInspectionItem.Chipping },
            { EResultType.Burr, BgaInspectionItem.Burr },
            { EResultType.RejectMark, BgaInspectionItem.RejectMark },
            { EResultType.XOut, BgaInspectionItem.XOut },
            { EResultType.XOut2, BgaInspectionItem.XOut2 }
        };

        public readonly Dictionary<EResultType, MoldInspectionItem> Mold = new Dictionary<EResultType, MoldInspectionItem>
        {
            { EResultType.NoDevice, MoldInspectionItem.NoDevice},
            { EResultType.PackageSize, MoldInspectionItem.PackageSize},
            { EResultType.PackageOffset, MoldInspectionItem.PackageOffset},
            { EResultType.NoMark, MoldInspectionItem.NoMark},
            { EResultType.MarkCount, MoldInspectionItem.MarkCount},
            { EResultType.WrongMark, MoldInspectionItem.WrongMark},
            { EResultType.TextAngle, MoldInspectionItem.TextAngle},
            { EResultType.TextOffset, MoldInspectionItem.TextOffset},
            { EResultType.DataCode, MoldInspectionItem.DataCode},
            { EResultType.MissingChar, MoldInspectionItem.MissingChar},
            { EResultType.Scratch, MoldInspectionItem.Scratch},
            { EResultType.ForeignMaterial, MoldInspectionItem.ForeignMaterial},
            { EResultType.Contamination, MoldInspectionItem.Contamination},
            { EResultType.SawOffset, MoldInspectionItem.SawOffset},
            { EResultType.Chipping, MoldInspectionItem.Chipping},
            { EResultType.Burr, MoldInspectionItem.Burr},
            { EResultType.RejectMark, MoldInspectionItem.RejectMark},
            { EResultType.CornerDegree, MoldInspectionItem.CornerDegree},
            { EResultType.XOut, MoldInspectionItem.XOut },
            { EResultType.XOut2, MoldInspectionItem.XOut2 }
        };

        public readonly Dictionary<EResultType, QfnInspectionItem> Qfn = new Dictionary<EResultType, QfnInspectionItem>
        {
            { EResultType.NoDevice, QfnInspectionItem.NoDevice },
            { EResultType.PackageOffset, QfnInspectionItem.PackageOffset },
            { EResultType.PackageSize, QfnInspectionItem.PackageSize },
            { EResultType.CornerDegree, QfnInspectionItem.CornerDegree },
            { EResultType.FirstPin, QfnInspectionItem.FirstPin },
            { EResultType.PadSize, QfnInspectionItem.PadSize },
            { EResultType.PadArea, QfnInspectionItem.PadArea },
            { EResultType.LeadCount, QfnInspectionItem.LeadCount },
            { EResultType.LeadSize, QfnInspectionItem.LeadSize },
            { EResultType.LeadArea, QfnInspectionItem.LeadArea },
            { EResultType.LeadPitch, QfnInspectionItem.LeadPitch },
            { EResultType.LeadOffset, QfnInspectionItem.LeadOffset },
            { EResultType.LeadContamination, QfnInspectionItem.LeadContamination },
            { EResultType.LeadPerimeter, QfnInspectionItem.LeadPerimeter },
            { EResultType.Scratch, QfnInspectionItem.Scratch },
            { EResultType.ForeignMaterial, QfnInspectionItem.ForeignMaterial },
            { EResultType.Contamination, QfnInspectionItem.Contamination },
            { EResultType.SawOffset, QfnInspectionItem.SawOffset },
            { EResultType.Chipping, QfnInspectionItem.Chipping },
            { EResultType.Burr, QfnInspectionItem.Burr },
            { EResultType.RejectMark, QfnInspectionItem.RejectMark },
            { EResultType.XOut, QfnInspectionItem.XOut },
        };

        public readonly Dictionary<EResultType, LgaInspectionItem> Lga = new Dictionary<EResultType, LgaInspectionItem>
        {
            { EResultType.NoDevice, LgaInspectionItem.NoDevice },
            { EResultType.PackageOffset, LgaInspectionItem.PackageOffset },
            { EResultType.PackageSize, LgaInspectionItem.PackageSize },
            { EResultType.CornerDegree, LgaInspectionItem.CornerDegree },
            { EResultType.FirstPin, LgaInspectionItem.FirstPin },
            { EResultType.MultiPadCount, LgaInspectionItem.MultiPadCount },
            { EResultType.MultiPadSize, LgaInspectionItem.MultiPadSize },
            { EResultType.MultiPadArea, LgaInspectionItem.MultiPadArea },
            { EResultType.MultiPadPitch, LgaInspectionItem.MultiPadPitch },
            { EResultType.MultiPadOffset, LgaInspectionItem.MultiPadOffset },
            { EResultType.MultiPadContamination, LgaInspectionItem.MultiPadContamination },
            { EResultType.MultiPadPerimeter, LgaInspectionItem.MultiPadPerimeter },
            { EResultType.LeadCount, LgaInspectionItem.LeadCount },
            { EResultType.LeadSize, LgaInspectionItem.LeadSize },
            { EResultType.LeadArea, LgaInspectionItem.LeadArea },
            { EResultType.LeadPitch, LgaInspectionItem.LeadPitch },
            { EResultType.LeadOffset, LgaInspectionItem.LeadOffset },
            { EResultType.LeadContamination, LgaInspectionItem.LeadContamination },
            { EResultType.LeadPerimeter, LgaInspectionItem.LeadPerimeter },
            { EResultType.Scratch, LgaInspectionItem.Scratch },
            { EResultType.ForeignMaterial, LgaInspectionItem.ForeignMaterial },
            { EResultType.Contamination, LgaInspectionItem.Contamination },
            { EResultType.SawOffset, LgaInspectionItem.SawOffset },
            { EResultType.Chipping, LgaInspectionItem.Chipping },
            { EResultType.Burr, LgaInspectionItem.Burr },
            { EResultType.RejectMark, LgaInspectionItem.RejectMark },
            { EResultType.XOut, LgaInspectionItem.XOut },
        };
    }
}