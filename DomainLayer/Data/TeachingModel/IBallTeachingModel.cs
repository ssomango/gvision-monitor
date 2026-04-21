using CommunityToolkit.Mvvm.ComponentModel;
using GVisionWpf.DomainLayer.Extensions;


namespace GVisionWpf.DomainLayer.Data.TeachingModel
{
    public interface IBallTeachingModel<T> where T : InspectionTeaching
    {
        IList<Roi> BallRois { get; set; }

        Threshold BallThreshold { get; set; }

        List<Circle> Balls { get; set; }

        double BallMinArea { get; set; }

        double BallMaxArea { get; set; }

        int BallMinCircularity { get; set; }

        double BallPositionOffset { get; set; }

        double BallMinSize { get; set; }

        double BallMaxSize { get; set; }
        double BallAvgDiameters { get; set; }
        Length BallAvgPitch { get; set; }

        Dictionary<string, List<Circle>> BallsByRoi { get; set; } 
        Dictionary<string, double> BallDiametersByRoi { get; set; }
        Dictionary<string, Length> BallPitchesByRoi { get; set; }


        public IBallTeachingModel<T> MergeTo(IBallTeachingModel<T> model)
        {
            model.BallRois.CopyFrom(BallRois);

            model.BallThreshold.CopyFrom(BallThreshold);

            model.Balls = Balls;
            model.BallMinArea = BallMinArea;
            model.BallMaxArea = BallMaxArea;
            model.BallMinCircularity = BallMinCircularity;
            model.BallPositionOffset = BallPositionOffset;

            model.BallMinSize = BallMinSize;
            model.BallMaxSize = BallMaxSize;

            model.BallAvgDiameters = BallAvgDiameters;
            model.BallAvgPitch = BallAvgPitch;

            model.BallsByRoi = BallsByRoi;
            model.BallDiametersByRoi = BallDiametersByRoi;
            model.BallPitchesByRoi = BallPitchesByRoi;

            return model;
        }
    }
}
