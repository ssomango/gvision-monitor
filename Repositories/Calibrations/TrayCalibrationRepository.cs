using GVisionWpf.Models.Entities.Recipe.Calibrations;
using System;

namespace GVisionWpf.Repositories.Calibrations
{
    public class TrayCalibrationRepository : CalibrationRepository<TrayCalibrationTeaching>
    {
        private static readonly Lazy<TrayCalibrationRepository> lazy = new Lazy<TrayCalibrationRepository>(() => new TrayCalibrationRepository());
        public static TrayCalibrationRepository Instance => lazy.Value;

        private TrayCalibrationRepository() : base("TRAY.cal") { }
    }
}