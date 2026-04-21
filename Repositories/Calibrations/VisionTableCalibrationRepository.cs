using GVisionWpf.Models.Entities.Recipe.Calibrations;
using System;

namespace GVisionWpf.Repositories.Calibrations
{
    public class VisionTableCalibrationRepository : CalibrationRepository<VisionTableCalibrationTeaching>
    {
        private static readonly Lazy<VisionTableCalibrationRepository> lazy = new Lazy<VisionTableCalibrationRepository>(() => new VisionTableCalibrationRepository());
        public static VisionTableCalibrationRepository Instance => lazy.Value;

        private VisionTableCalibrationRepository() : base("VISION_TABLE.cal") { }
    }
}