using GVisionWpf.Models.Visions;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class BottomJigCalibrationTeaching : CommonCalibrationTeaching
    {
        public BottomJigCalibrationTeaching()
        {
            this.Roi = new Roi("JIG (BOTTOM)", 0, 0, 2040, 2048);
        }
    }
}