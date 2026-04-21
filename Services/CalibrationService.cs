using System;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories.Calibrations;
using GVisionWpf.Types;
using GVisionWpf.Visions.Engines;
using HalconDotNet;
using QcJigCalibrationTeaching = GVisionWpf.Models.Entities.Recipe.Calibrations.QcJigCalibrationTeaching;

namespace GVisionWpf.Services
{
    public class CalibrationService
    {
        private static readonly Lazy<CalibrationService> lazy = new Lazy<CalibrationService>(() => new CalibrationService());
        public static CalibrationService Instance => lazy.Value;

        private readonly BottomJigCalibrationRepository bottomJigRepository = BottomJigCalibrationRepository.Instance;
        private readonly SettingJigCalibrationRepository settingJigRepository = SettingJigCalibrationRepository.Instance;
        private readonly PadPitchCalibrationRepository padPitchRepository = PadPitchCalibrationRepository.Instance;
        private readonly TrayCalibrationRepository trayRepository = TrayCalibrationRepository.Instance;
        private readonly VisionTableCalibrationRepository visionTableRepository = VisionTableCalibrationRepository.Instance;

        private CalibrationService() { }

        public CalibrationResult CalculatePickerOffset(HObject image, ECamera camera)
        {
            PadPitchCalibrationTeaching teaching = this.padPitchRepository.GetRecipe();

            return CalibrationEngine.CalibrationEngineBase(image, teaching.Roi, teaching.Threshold, teaching.MinSize, teaching.MaxSize, teaching.Similarity, teaching.ShapeType, teaching.StandardType, ECalibration.PadPitch, camera);
        }

        public CalibrationResult CalculateBottomJigOffset(HObject image, ECamera camera)
        {
            BottomJigCalibrationTeaching teaching = this.bottomJigRepository.GetRecipe();

            return CalibrationEngine.CalibrationEngineBase(image, teaching.Roi, teaching.Threshold, teaching.MinSize, teaching.MaxSize, teaching.Similarity, teaching.ShapeType, teaching.StandardType, ECalibration.BottomJig, camera);
        }

        public CalibrationResult CalculateSettingZigOffset(HObject image, ECamera camera)
        {
            SettingJigCalibrationTeaching teaching = this.settingJigRepository.GetRecipe();

            return CalibrationEngine.CalibrationEngineBase(image, teaching.Roi, teaching.Threshold, teaching.MinSize, teaching.MaxSize, teaching.Similarity, teaching.ShapeType, teaching.StandardType, ECalibration.SettingJig, camera);
        }

        public CalibrationResult CalculateTrayTransferOffset(HObject image, ECamera camera)
        {
            TrayCalibrationTeaching teaching = this.trayRepository.GetRecipe();

            return CalibrationEngine.CalibrationEngineBase(image, teaching.Roi, teaching.Threshold, teaching.MinSize, teaching.MaxSize, teaching.Similarity, teaching.ShapeType, teaching.StandardType, ECalibration.Tray, camera);
        }

        public CalibrationResult CalculateVisionTableOffset(HObject image, ECamera camera)
        {
            VisionTableCalibrationTeaching teaching = this.visionTableRepository.GetRecipe();

            return CalibrationEngine.CalibrationEngineBase(image, teaching.Roi, teaching.Threshold, teaching.MinSize, teaching.MaxSize, teaching.Similarity, teaching.ShapeType, teaching.StandardType, ECalibration.VisionTable, camera);
        }
    }
}