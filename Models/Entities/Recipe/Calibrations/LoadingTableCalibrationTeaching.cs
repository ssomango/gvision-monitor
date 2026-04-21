using GVisionWpf.Models.Visions;
using GVisionWpf.Types;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class LoadingTableCalibrationTeaching : CommonCalibrationTeaching
    {
        public LoadingTableCalibrationTeaching()
        {
            this.Roi = new Roi("LOADING TABLE", 0, 0, 2040, 2048);
            this.ShapeType = EShape.Circle;
            this.StandardType = ECalibrationStandard.Center;
        }
    }
}