using GVisionWpf.Models.Visions;
using GVisionWpf.Types;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class SettingJigCalibrationTeaching : CommonCalibrationTeaching
    {
        public SettingJigCalibrationTeaching()
        {
            this.Roi = new Roi("JIG (TOP)", 0, 0, 2040, 2048);
            this.ShapeType = EShape.Rectangle;
            this.StandardType = ECalibrationStandard.Biggest;
        }
    }
}