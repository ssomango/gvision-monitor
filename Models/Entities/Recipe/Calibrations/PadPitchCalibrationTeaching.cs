using GVisionWpf.Models.Visions;

namespace GVisionWpf.Models.Entities.Recipe.Calibrations
{
    public class PadPitchCalibrationTeaching : CommonCalibrationTeaching
    {
        public PadPitchCalibrationTeaching()
        {
            this.Roi = new Roi("PAD", 0, 0, 2040, 2048);
        }
    }
}