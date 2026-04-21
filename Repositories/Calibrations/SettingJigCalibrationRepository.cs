using GVisionWpf.Models.Entities.Recipe.Calibrations;
using System;

namespace GVisionWpf.Repositories.Calibrations
{
    public class SettingJigCalibrationRepository : CalibrationRepository<SettingJigCalibrationTeaching>
    {
        private static readonly Lazy<SettingJigCalibrationRepository> lazy = new Lazy<SettingJigCalibrationRepository>(() => new SettingJigCalibrationRepository());
        public static SettingJigCalibrationRepository Instance => lazy.Value;

        private SettingJigCalibrationRepository() : base("SETTING_JIG.cal") { }
    }
}