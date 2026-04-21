using AnyDiff;
using AnyDiff.Extensions;
using GVisionWpf.Dialects.Jsons;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.ViewModels;
using System.Windows;

namespace GVisionWpf.GlobalStates
{
    internal class GlobalSetting
    {
        private static GlobalSetting? instance;
        public ESystemType SystemType = ESystemType.HanaMicron;
        
        private static readonly object lockObject = new object();

        private const string GLOBAL_SETTING_FOLDER_PATH = "DB";
        private const string DEVICE_INFO_JSON_FILE_NAME = "device_info.json";
        private const string INSPECTION_JSON_FILE_NAME = "inspection.json";
        private const string TEST_IMAGE_JSON_FILE_NAME = "test_images.json";
        private const string CAMERA_FILE_NAME = "camera.json";
        private const string ILLUMINATION_FILE_NAME = "illumination.json";
        private const string LIGHT_CONTROLLER_FILE_NAME = "light_serial.json";
        private const string CONTROLLER_INFO_FILE_NAME = "controller_info.json";
        private const string ECAMERA_NO_FILE_NAME = "ecamera_no.json";
        private const string CAMERA_DOMAIN_NAME = "camera_domain.json";
        private const string PICKER_OFFSET_COMPENSATION_NAME = "picker_offset_compensation.json";
        private const string VISION_RESULT_FILE_NAME = "vision_result.json";


        public Visibility MoldInspectionVisibility = Visibility.Visible;
        public Visibility BgaInspectionVisibility = Visibility.Visible;
        public Visibility LgaInspectionVisibility = Visibility.Visible;
        public Visibility QfnInspectionVisibility = Visibility.Visible;


        public DeviceInfo DeviceInfo { get; set; }
        public Inspection Inspection { get; set; }

        public List<CameraInfo> CameraInfos { get; set; }
        public List<CameraLightInfo> LightInfos { get; set; }
        public List<LightControllerInfo> LightControllerInfos { get; set; }
        public Dictionary<string, ControllerInfo> ControllerInfos { get; set; } = new Dictionary<string, ControllerInfo>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<uint, ECamera> ECameraNos { get; set; }
        public TestImage TestImage { get; set; }
        public Dictionary<ECamera, Resolution> CameraDomain { get; set; }
        public Dictionary<EMultiPicker, List<Pose>> OffsetCompensation { get; set; }
        public Dictionary<EInspection, Dictionary<EResultType, uint>> VisionResult { get; set; }
        public ERunningMode CurrentRunningMode { get; set; } = ERunningMode.SetUp;

        private GlobalSetting()
        {
            /* 기본값을 생성해주는 코드를 넣지 마세요. 디버깅을 어렵게 만들 수 있습니다.
             * 필요한 세팅값이 없는 경우, 차라리 오류를 throw하세요. 이것이 훨씬 더 안전합니다. 무언가를 기본값으로 숨기지 마세요.
             * 그래야 디버깅할 수 있습니다. 기본적으로 모든 세팅 값은 설정 파일에 올바르게 세팅되어있다고 가정해야합니다.
             */
        }

        public static GlobalSetting Instance
        {
            get
            {
                lock (lockObject)
                {
                    if (instance != null)
                    {
                        return instance;
                    }

                    instance ??= LoadFromFile();
                    return instance;
                }
            }
        }

        public void ApplySetting()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                CurrentSettingViewmodel.Instance.InspectionMode = Inspection.Mode;
                CurrentSettingViewmodel.Instance.SaveOption = Inspection.SaveOption;
                CurrentSettingViewmodel.Instance.RecipeName = DeviceInfo.RecipeName;
                CurrentSettingViewmodel.Instance.LotNumber = DeviceInfo.LotNumber;

                PrsResultViewViewModel.Instance.Initialize();
                MapResultViewViewModel.Instance.Initialize();

                Device currentDevice = DeviceRecipeRepository.Instance.GetRecipe();
                PrsDeviceViewViewModel.Instance.VisionTableLayout = currentDevice.TraySize;
                PrsDeviceViewViewModel.Instance.FovLayout = new TableLayout(1, 1);
                PrsDeviceViewViewModel.Instance.BlockLayout = currentDevice.BlockSize;
                MapDeviceViewViewModel.Instance.VisionTableLayout = currentDevice.TraySize;
                MapDeviceViewViewModel.Instance.FovLayout = currentDevice.FovSize;
                MapDeviceViewViewModel.Instance.BlockLayout = currentDevice.BlockSize;

                X1PickerDeviceViewViewModel.Instance.ClearResults();
                X2PickerDeviceViewViewModel.Instance.ClearResults();
            });
        }

        public void Persist()
        {
            JsonDialect.Instance.Create(GLOBAL_SETTING_FOLDER_PATH, DEVICE_INFO_JSON_FILE_NAME, DeviceInfo);
            JsonDialect.Instance.Create(GLOBAL_SETTING_FOLDER_PATH, INSPECTION_JSON_FILE_NAME, Inspection);
            JsonDialect.Instance.Create(GLOBAL_SETTING_FOLDER_PATH, TEST_IMAGE_JSON_FILE_NAME, TestImage);
        }

        public static GlobalSetting LoadFromFile()
        {
            GlobalSetting setting = new GlobalSetting();
            DeviceInfo deviceInfo = JsonDialect.Instance.Read<DeviceInfo>(GLOBAL_SETTING_FOLDER_PATH, DEVICE_INFO_JSON_FILE_NAME);
            Inspection inspection = JsonDialect.Instance.Read<Inspection>(GLOBAL_SETTING_FOLDER_PATH, INSPECTION_JSON_FILE_NAME);
            List<CameraInfo> cameraInfos = JsonDialect.Instance.Read<List<CameraInfo>>(GLOBAL_SETTING_FOLDER_PATH, CAMERA_FILE_NAME);
            List<CameraLightInfo> lightInfos = JsonDialect.Instance.Read<List<CameraLightInfo>>(GLOBAL_SETTING_FOLDER_PATH, ILLUMINATION_FILE_NAME);
            List<LightControllerInfo> lightControllerInfos = JsonDialect.Instance.Read<List<LightControllerInfo>>(GLOBAL_SETTING_FOLDER_PATH, LIGHT_CONTROLLER_FILE_NAME);
            TestImage testImage = JsonDialect.Instance.Read<TestImage>(GLOBAL_SETTING_FOLDER_PATH, TEST_IMAGE_JSON_FILE_NAME);
            List<ControllerInfo> controllerInfos = JsonDialect.Instance.Read<List<ControllerInfo>>(GLOBAL_SETTING_FOLDER_PATH, CONTROLLER_INFO_FILE_NAME);
            Dictionary<uint, ECamera> eCameraNos = JsonDialect.Instance.Read<Dictionary<uint, ECamera>>(GLOBAL_SETTING_FOLDER_PATH, ECAMERA_NO_FILE_NAME);
            Dictionary<ECamera, Resolution> cameraDomain = JsonDialect.Instance.Read<Dictionary<ECamera, Resolution>>(GLOBAL_SETTING_FOLDER_PATH, CAMERA_DOMAIN_NAME);
            Dictionary<EMultiPicker, List<Pose>> offsetCompensation = JsonDialect.Instance.Read<Dictionary<EMultiPicker, List<Pose>>>(GLOBAL_SETTING_FOLDER_PATH, PICKER_OFFSET_COMPENSATION_NAME);
            Dictionary<EInspection, Dictionary<EResultType, uint>> visionResult = JsonDialect.Instance.Read<Dictionary<EInspection, Dictionary<EResultType, uint>>>(GLOBAL_SETTING_FOLDER_PATH, VISION_RESULT_FILE_NAME);

            setting.DeviceInfo = deviceInfo;
            setting.Inspection = inspection;
            setting.Inspection.DBSaveDays = inspection.DBSaveDays == 0 ? 30 : inspection.DBSaveDays;

            setting.CameraInfos = cameraInfos;
            setting.LightInfos = lightInfos;
            setting.TestImage = testImage;
            setting.LightControllerInfos = lightControllerInfos;
            setting.ControllerInfos = controllerInfos.ToDictionary(ci => ci.TaskName, StringComparer.OrdinalIgnoreCase);
            setting.ECameraNos = eCameraNos;
            setting.CameraDomain = cameraDomain;
            setting.OffsetCompensation = offsetCompensation;
            setting.VisionResult = visionResult;

            return setting;
        }

        public static ICollection<Difference> GetDiffOfPreviousVersion()
        {
            GlobalSetting previousVersion = LoadFromFile();
            return previousVersion.Diff(GlobalSetting.Instance);
        }
    }
}