using GVisionWpf.Models.Visions;
using GVisionWpf.Types;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class TrayCalibrationTeaching : CommonCalibrationTeaching
    {
        public TrayCalibrationTeaching()
        {
            this.Roi = new Roi("TRAY", 0, 0, 2040, 2048);
            this.ShapeType = EShape.Rectangle;
            this.StandardType = ECalibrationStandard.Center;
        }
    }
}
