using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Api;
using GVisionWpf.Cameras;
using GVisionWpf.Dialects.Jsons;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.Models.UiModels;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using GVisionWpf.UIs.Overlays;
using GVisionWpf.UIs.ViewModels.Teaching;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace GVisionWpf.UIs.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const string GLOBAL_SETTING_FOLDER_PATH = "DB";
        private const string MAIN_WINDOW_FILE_NAME = "sidebar.json";
        private const string LOGO_FOLDER_PATH = "/Assets/Icons/Logos/";

        private SystemUsageMonitorWindow? systemWindow;
        private readonly MainWindowLayout mainWindowLayout;
        private Dictionary<string, Action> commands;
        private readonly WindowService _windowService;
        public static SettingsViewModel Instance { get; } = new SettingsViewModel();

        #region Property

        public string LogoImagePath => AppDomain.CurrentDomain.BaseDirectory + LOGO_FOLDER_PATH + this.mainWindowLayout.LogoImageName;
        public string TitleName => this.mainWindowLayout.TitleName;

        public ObservableCollection<SidebarTab> SidebarTabs => this.mainWindowLayout.SidebarTabs;

        #endregion

        public MainWindowViewModel()
        {
            _windowService = new WindowService();
            ApiController.Instance.MainViewModel = this;
            try
            {
                this.mainWindowLayout = JsonDialect.Instance.Read<MainWindowLayout>(GLOBAL_SETTING_FOLDER_PATH, MAIN_WINDOW_FILE_NAME);

                var teachings = mainWindowLayout.SidebarTabs.Where(tab => tab.TabName.ToLower() == "TEACHING".ToLower())
                    .First()
                    .Buttons
                    .Select(b => b.Name.ToLower());

                GlobalSetting.Instance.MoldInspectionVisibility = teachings.Any(t => t.Contains("Mold".ToLower())) ? Visibility.Visible : Visibility.Collapsed;
                GlobalSetting.Instance.BgaInspectionVisibility = teachings.Any(t => t.Contains("Bga".ToLower())) ? Visibility.Visible : Visibility.Collapsed;
                GlobalSetting.Instance.LgaInspectionVisibility = teachings.Any(t => t.Contains("Lga".ToLower())) ? Visibility.Visible : Visibility.Collapsed;
                GlobalSetting.Instance.QfnInspectionVisibility = teachings.Any(t => t.Contains("Qfn".ToLower())) ? Visibility.Visible : Visibility.Collapsed;

                this.commands = new Dictionary<string, Action>
                {
                    { "showGridMoldTeaching", showGridMoldTeaching },
                    { "showBgaTeaching", showBgaTeaching },
                    { "showQfnTeaching", showQfnTeaching },
                    { "showLgaTeaching", showLgaTeaching },
                    { "showMoldTeaching", showMoldTeaching },
                    { "showGridBgaTeaching", showGridBgaTeaching },
                    { "showGridLgaTeaching", showGridLgaTeaching },
                    { "showGridQfnTeaching", showGridQfnTeaching },
                    { "showStripTeaching", showStripTeaching },
                    { "showLight", showLight },
                    { "showSystemUsageMonitor", showSystemUsageMonitor },
                    { "showLotData", showLotData },
                    { "showSettings", showSettings },
                    { "showCalibration", showCalibration },
                    { "showHistory", showHistory },
                    { "showAsSupport", showAs },
                    { "exitApplication", exit }
                };
            }
            catch
            {
                throw new GFileNotFoundException(GLOBAL_SETTING_FOLDER_PATH, MAIN_WINDOW_FILE_NAME);
            }

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                this.mainWindowLayout = new MainWindowLayout
                {
                    TitleName = "GVision",
                    LogoImageName = "deepseers.png",
                    SidebarTabs = new ObservableCollection<SidebarTab>
                    {
                        new SidebarTab
                        {
                            IsExpanded = true,
                            TabName = "TEST",
                            Buttons = new ObservableCollection<SidebarButton>
                            {
                                new SidebarButton
                                {
                                    Name = "History",
                                    CommandName = "showHistory",
                                    IconPath = "/Assets/Icons/Sidebar/history.png",
                                }
                            }
                        }
                    }
                };
            }

            loadCommands();
        }

        private void loadCommands()
        {
            foreach (SidebarTab tab in this.mainWindowLayout.SidebarTabs)
            {
                foreach (SidebarButton button in tab.Buttons)
                {
                    button.Command = getCommandByButton(button);
                }
            }
        }

        private ICommand getCommandByButton(SidebarButton button)
        {
            return new RelayCommand(() =>
            {
                Action? command;
                if (!this.commands.TryGetValue(button.CommandName, out command))
                {
                    command = () => { MessageBox.Show($"Unknown command: {button.CommandName}"); };
                }

                var currentMode = GlobalSetting.Instance.CurrentRunningMode;
                var allowedModes = new (ERunningMode Mode, bool IsAllowed)[]
                {
                    (ERunningMode.Run, button.IsRunModeAllowed),
                    (ERunningMode.SetUp, button.IsSetUpModeAllowed)
                };

                if (allowedModes.Any(x => x.IsAllowed && x.Mode == currentMode))
                {
                    command();
                }
                else
                {
                    new AlertWindow("Notification", AlertWindow.EIcon.ALERT, "This feature is not available in the current mode. Please switch the mode.", AlertWindow.EAlert.YES).ShowDialog();
                }

            });
        }

        private void showGridMoldTeaching()
        {
            if (MappingTeachingStartFlow.TryHandleSidebarMappingClick())
            {
                Debug.WriteLine("[MappingStartFlow] sidebar mapping click detected during training flow; continuing command execution.");
            }

            _ = WindowManager.OpenOrActivateAsync<GridMoldTeachingWindow>();
        }

        private void showBgaTeaching()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "TeachingImage Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            HOperatorSet.ReadImage(out HObject teachingImage, openFileDialog.FileName);

            new BgaTeachingWindow { TeachingImage = teachingImage }.ShowDialog();
        }

        private void showQfnTeaching()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "TeachingImage Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            HOperatorSet.ReadImage(out HObject teachingImage, openFileDialog.FileName);

            new QfnTeachingWindow { TeachingImage = teachingImage }.ShowDialog();
        }

        private void showLgaTeaching()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "TeachingImage Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            HOperatorSet.ReadImage(out HObject teachingImage, openFileDialog.FileName);

            new LgaTeachingWindow { TeachingImage = teachingImage }.ShowDialog();
        }

        private void showMoldTeaching()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "TeachingImage Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            HOperatorSet.ReadImage(out HObject teachingImage, openFileDialog.FileName);

            new MoldTeachingWindow { TeachingImage = teachingImage }.ShowDialog();
        }

        private void showGridBgaTeaching()
        {
            int nShots = IlluminationService.Instance.GetShotCount(ECamera.Mapping);
            ObservableCollection<HObject> shots = new ObservableCollection<HObject>();

            for (int i = 1; i <= nShots; i++)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = $"Mapping Shot# {i} Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    new AlertWindow("Image Selection Error", AlertWindow.EIcon.ALERT, "Please select the same number of images as the number of shots.", AlertWindow.EAlert.YES).ShowDialog();
                    return;
                }

                HOperatorSet.ReadImage(out HObject image, openFileDialog.FileName);
                shots.Add(image);
            }

            new GridBgaTeachingWindow(shots).ShowDialog();
        }

        private void showGridLgaTeaching()
        {
            int nShots = IlluminationService.Instance.GetShotCount(ECamera.Mapping);
            ObservableCollection<HObject> shots = new ObservableCollection<HObject>();

            for (int i = 1; i <= nShots; i++)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = $"Mapping Shot# {i} Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    new AlertWindow("Image Selection Error", AlertWindow.EIcon.ALERT, "Please select the same number of images as the number of shots.", AlertWindow.EAlert.YES).ShowDialog();
                    return;
                }

                HOperatorSet.ReadImage(out HObject image, openFileDialog.FileName);
                shots.Add(image);
            }

            new GridLgaTeachingWindow(shots).ShowDialog();
        }

        private void showGridQfnTeaching()
        {
            int nShots = IlluminationService.Instance.GetShotCount(ECamera.Mapping);
            ObservableCollection<HObject> shots = new ObservableCollection<HObject>();

            for (int i = 1; i <= nShots; i++)
            {
                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = $"Mapping Shot# {i} Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    new AlertWindow("Image Selection Error", AlertWindow.EIcon.ALERT, "Please select the same number of images as the number of shots.", AlertWindow.EAlert.YES).ShowDialog();
                    return;
                }

                HOperatorSet.ReadImage(out HObject image, openFileDialog.FileName);
                shots.Add(image);
            }

            new GridQfnTeachingWindow(shots).ShowDialog();
        }

        private void showStripTeaching()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "TeachingImage Files (*.jpg *png) | *.jpg; *png; | All files (*.*) | *.*";

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            HOperatorSet.ReadImage(out HObject teachingImage, openFileDialog.FileName);

            new StripTeachingWindow() { TeachingImage = teachingImage }.ShowDialog();
        }

        private void showSystemUsageMonitor()
        {
            this.systemWindow ??= new SystemUsageMonitorWindow();

            if (!this.systemWindow.IsLoaded || !this.systemWindow.IsVisible)
            {
                this.systemWindow.Show();
            }
            else
            {
                this.systemWindow.Hide();
            }
        }

        private void showLight() => new LightWindow().ShowDialog();

        private void showLotData() => new LotWindow().ShowDialog();

        private void showSettings() => new SettingsWindow().ShowDialog();

        private void showCalibration() => new CalibrationWindow().Show();

        private void showHistory() => new HistoryWindow().ShowDialog();

        //private void showAs() => new ASWindow().ShowDialog();

        private void showAs()
        {
            var window = new ASWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        //public void foo()
        //{
        //    string query = "pckage width is 10.2";

        //    // llm

        //    string viewModelToChange = "GVisionWpf.UIs.ViewModels.SettingsViewModel";
        //    string variableNameToChange = "PackageWidth";
        //    string value = "10.2";

        //    // 타입 찾기
        //    Type type = Type.GetType(viewModelToChange);
        //    if (type == null)
        //    {
        //        Console.WriteLine("타입을 찾을 수 없음");
        //        return;
        //    }

        //    // 정적 프로퍼티 Instance 가져오기
        //    PropertyInfo instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        //    object instance = instanceProp.GetValue(null);

        //    // 프로퍼티 접근
        //    PropertyInfo prop = type.GetProperty(variableNameToChange);
        //    if (prop == null)
        //    {
        //        Console.WriteLine("프로퍼티를 찾을 수 없음");
        //        return;
        //    }

        //    // 문자열을 적절한 타입으로 변환
        //    object convertedValue = Convert.ChangeType(value, prop.PropertyType);

        //    // 값 설정
        //    prop.SetValue(instance, convertedValue);

        //    // 확인
        //    Console.WriteLine(prop.GetValue(instance)); // 10.2
        //}


        public void exit()
        {
            bool? isExit = new AlertWindow("Exit", "Are you sure you want to exit?", AlertWindow.EAlert.YESNO).ShowDialog();

            if (!isExit.GetValueOrDefault())
            {
                return;
            }

            LightManager.Instance.TurnOffAllLightsFromAllCamera();
            Application.Current.MainWindow?.Close();
            Communicator.Instance.ReleaseClient();
            Heart.Instance.Stop();
            CameraManager.Instance.StopAllLiveSource();
            CameraManager.Instance.ReleaseAllFrameGrabber();
            CameraManager.Instance.ResetTriggerListener();
            Environment.Exit(0);
            //Process.GetCurrentProcess().Kill(); 최후의 종료수단
        }
    }
}
