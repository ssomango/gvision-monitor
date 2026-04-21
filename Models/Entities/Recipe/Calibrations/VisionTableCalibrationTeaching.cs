using GVisionWpf.Models.Visions;
using GVisionWpf.Types;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class VisionTableCalibrationTeaching : CommonCalibrationTeaching
    {
        public VisionTableCalibrationTeaching()
        {
            this.Roi = new Roi("VISION TABLE", 0, 0, 2040, 2048);
            this.ShapeType = EShape.Circle;
            this.StandardType = ECalibrationStandard.Center;
        }
    }
}