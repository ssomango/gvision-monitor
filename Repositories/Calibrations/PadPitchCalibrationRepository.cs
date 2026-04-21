using GVisionWpf.Models.Entities.Recipe.Calibrations;

namespace GVisionWpf.Repositories.Calibrations
{
    public class PadPitchCalibrationRepository : CalibrationRepository<PadPitchCalibrationTeaching>
    {
        private static readonly Lazy<PadPitchCalibrationRepository> lazy = new Lazy<PadPitchCalibrationRepository>(() => new PadPitchCalibrationRepository());
        public static PadPitchCalibrationRepository Instance => lazy.Value;

        private PadPitchCalibrationRepository() : base("PAD_PITCH.cal") { }
    }
}