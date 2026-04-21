using GVisionWpf.Models.Visions;
using GVisionWpf.Types;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class QcJigCalibrationTeaching : CommonCalibrationTeaching
    {
        public QcJigCalibrationTeaching()
        {
            this.Roi = new Roi("JIG (QC)", 0, 0, 2040, 2048);
            this.ShapeType = EShape.Circle;
            this.StandardType = ECalibrationStandard.Center;
        }
    }
}