using GVisionWpf.Models.Entities.Recipe.Calibrations;

namespace GVisionWpf.Repositories.Calibrations
{
    public class BottomJigCalibrationRepository : CalibrationRepository<BottomJigCalibrationTeaching>
    {
        private static readonly Lazy<BottomJigCalibrationRepository> lazy = new Lazy<BottomJigCalibrationRepository>(() => new BottomJigCalibrationRepository());
        public static BottomJigCalibrationRepository Instance => lazy.Value;

        private BottomJigCalibrationRepository() : base("BOTTOM_JIG.cal") { }
    }
}