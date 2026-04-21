using GVisionWpf.Models.Visions;
using GVisionWpf.Types;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class PressCalibrationTeaching : CommonCalibrationTeaching
    {
        public PressCalibrationTeaching()
        {
            this.Roi = new Roi("PRESS", 0, 0, 2040, 2048);
            this.ShapeType = EShape.Rectangle;
            this.StandardType = ECalibrationStandard.Biggest;
        }
    }
}