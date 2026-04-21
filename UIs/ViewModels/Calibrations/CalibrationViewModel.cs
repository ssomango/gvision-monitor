using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Cameras;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.Models.Entities.Recipe;
using GVisionWpf.Models.Entities.Recipe.Calibrations;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.PresentationLayer.Communications;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.Types;
using GVisionWpf.UIs.Frames.Panels;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.Utils;
using GVisionWpf.Visions;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace GVisionWpf.UIs.ViewModels.Calibrations
{
    public class CalibrationViewModel : ViewModelBase
    {
        private static readonly Lazy<CalibrationViewModel> lazy = new Lazy<CalibrationViewModel>(() => new CalibrationViewModel());
        public static CalibrationViewModel Instance => lazy.Value;

        public ObservableCollection<CalibrationTabViewModel> TabViewModels { get; } = new ObservableCollection<CalibrationTabViewModel>();
        private CalibrationTabViewModel? selectedTabViewModel;
        private readonly Dictionary<ECalibration, int> tabInfos = new Dictionary<ECalibration, int>();
        private int selectedTabIndex;

        private ObservableCollection<CalibrationResult> results = new ObservableCollection<CalibrationResult>();
        private CalibrationResult? selectedResult;

        public bool IsWindowOpened = false;
        public event EventHandler<object>? ScrollToNewItemRequested;
        private ECalibration? postCalibrationType;
        private EReticleType reticleType = EReticleType.Default;

        // Light Control
        private ECamera camera;
        private ECamera cameraSelectedValue;
        private int cameraSelectedIndex;
        private ObservableCollection<LightControllerPanelViewModel> lightControllerCollection = new ObservableCollection<LightControllerPanelViewModel>();
        private ObservableCollection<dynamic> dataGridItems = new ObservableCollection<dynamic>();
        private List<ELight> eLights = new List<ELight>();

        private LightManager lightManager = LightManager.Instance;
        private IlluminationService illuminationService = IlluminationService.Instance;
        private IlluminationRecipe illuminationRecipe;
        private Debouncer debouncer = new Debouncer(Application.Current.Dispatcher);

        #region Property

        public ICommand CloseWindowCommand
        {
            get => new RelayCommand(closeWindow);
        }

        public ICommand ItemDoubleClickCommand
        {
            get => new RelayCommand(onItemDoubleClick);
        }

        public ICommand TestCommand
        {
            get => new AsyncRelayCommand(test);
        }

        public ICommand LightSaveCommand { get; private set; }

        public ICommand ReticleTypeCommand
        {
            get => new RelayCommand<EReticleType>(value => ReticleType = value);
        }

        public CalibrationTabViewModel? SelectedTabViewModel
        {
            get => this.selectedTabViewModel;
            set => SetField(ref this.selectedTabViewModel, value);
        }

        public int SelectedTabIndex
        {
            get => this.selectedTabIndex;
            set => SetField(ref this.selectedTabIndex, value);
        }

        public CalibrationResult SelectedResult
        {
            get => this.selectedResult!;
            set => this.selectedResult = value;
        }

        public ObservableCollection<CalibrationResult> Results
        {
            get => this.results;
            set
            {
                this.results = value;
                OnPropertyChanged();
            }
        }

        public EReticleType ReticleType
        {
            get => this.reticleType;
            set
            {
                this.reticleType = value;
                OnPropertyChanged();
            }
        }

        public IlluminationRecipe IlluminationRecipe
        {
            get => this.illuminationRecipe;
            set
            {
                this.illuminationRecipe = value;
                OnPropertyChanged();
            }
        }

        public ECamera Camera
        {
            get => this.camera;
            set
            {
                this.camera = value;
                OnPropertyChanged();
            }
        }

        public List<ELight> ELights
        {
            get => this.eLights;
            set
            {
                this.eLights = value;
                OnPropertyChanged();
            }
        }

        public System.Collections.IEnumerable CameraItemsSource
        {
            get => GlobalSetting.Instance.CameraInfos.Select(c => c.CameraType);
        }

        public ECamera CameraSelectedValue
        {
            get => this.cameraSelectedValue;
            set
            {
                this.cameraSelectedValue = value;
                OnPropertyChanged();
            }
        }

        public dynamic CameraSelectedIndex
        {
            get => this.cameraSelectedIndex;
            set
            {
                this.cameraSelectedIndex = value;
                OnPropertyChanged();

                LightControllerCollection.Clear();
                addLightController();
            }
        }

        public ObservableCollection<LightControllerPanelViewModel> LightControllerCollection
        {
            get => this.lightControllerCollection;
            set => SetField(ref this.lightControllerCollection, value);
        }

        public ObservableCollection<dynamic> DataGridItems
        {
            get => this.dataGridItems;
            set
            {
                this.dataGridItems = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public CalibrationViewModel()
        {
            initializeTabs();
            SelectedTabViewModel = TabViewModels.First();

            LightSaveCommand = new RelayCommand(lightSave);

            this.results.CollectionChanged += onItemsCollectionChanged;
        }

        private void initializeTabs()
        {
            TabViewModels.Add(new BottomJigCalibrationTabViewModel());
            TabViewModels.Add(new SettingJigCalibrationTabViewModel());
            TabViewModels.Add(new PadPitchCalibrationTabViewModel());
            TabViewModels.Add(new TrayCalibrationTabViewModel());
            TabViewModels.Add(new VisionTableCalibrationTabViewModel());
          

            for (int i = 0; i < TabViewModels.Count; i++)
            {
                CalibrationTabViewModel tabViewModel = TabViewModels[i];
                this.tabInfos[tabViewModel.CalibrationType] = i;
            }
        }

        public void AddResult(CalibrationResult result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Results.Add(result);
                SelectedTabIndex = this.tabInfos[result.CalibrationType];
                ScrollToNewItemRequested?.Invoke(this, result);
            });
        }

        private void displayResult(CalibrationResult result)
        {
            CurrentTeachingWindow.Instance.TeachingImage = result.Image;

            VisionWindow? window = CurrentTeachingWindow.Instance.Window;
            window?.Display(result.Image);

            if (this.postCalibrationType != result.CalibrationType)
            {
                window?.SetFullImagePart();
            }

            if (result.IsFound && result.Region != null)
            {
                window?.Display(result.Region, EColor.Red, "margin", 2);
            }

            if (this.reticleType != EReticleType.None)
            {
                double reticlePixelSize = ReticleType switch
                {
                    EReticleType.Default => CameraManager.Instance.Cameras![result.CameraType].PixelPerMillimeter + 1,
                    EReticleType.FullSize => 10_000,
                    _ => 0
                };

                VisionOperation.GenReticle(result.TargetPose, reticlePixelSize, out HObject targetReticle);
                window?.Display(targetReticle, EColor.Green, "fill", 2);
                targetReticle.Dispose();

                VisionOperation.GenReticle(result.ImageCenterPoint, reticlePixelSize, out HObject imageReticle);
                window?.Display(imageReticle, EColor.Cyan, "fill", 2);
                imageReticle.Dispose();
            }

            this.postCalibrationType = result.CalibrationType;
        }

        private async Task test()
        {
            if (SelectedTabViewModel == null)
            {
                return;
            }

            await SelectedTabViewModel.ExecuteCalibration();
        }

        private void closeWindow()
        {
            Heart.Instance.CurrentVisionMode = EVisionMode.Teaching;

            this.postCalibrationType = null;
            releaseResults();

            // 라이브 중이라면 현재 조명 값을 레시피의 값으로 변경
            LoadLightRecipe();
            foreach (var cameraInfo in GlobalSetting.Instance.CameraInfos)
            {
                ECamera ct = cameraInfo.CameraType;

                if (CameraManager.Instance.Cameras[ct].GetCountLiveObservers() > 0)
                {
                    this.lightManager.SetBrightness(ct, IlluminationRecipe.Setting[ct][0]);
                }
                else
                {
                    this.lightManager.TurnOffAllLights(ct);
                }
            }
        }

        private void releaseResults()
        {
            foreach (CalibrationResult result in this.results)
            {
                result.Image.Dispose();
                result.Region?.Dispose();
            }

            this.results.CollectionChanged -= onItemsCollectionChanged;
            this.results.Clear();
            this.results.CollectionChanged += onItemsCollectionChanged;
        }

        private void onItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }

            CalibrationResult newest = this.results.Last();
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => displayResult(newest))
                );

                if (!this.IsWindowOpened)
                {
                    new CalibrationWindow().ShowDialog();
                }
            }));
        }

        private void onItemDoubleClick()
        {
            if (this.selectedResult == null)
            {
                return;
            }

            displayResult(this.selectedResult);
        }

        public void LoadLightRecipe()
        {
            IlluminationRecipe = this.illuminationService.GetIlluminationRecipe();
        }

        public void LoadDataGridItems()
        {
            DataGridItems.Clear();

            IlluminationRecipe.Setting.TryGetValue(CameraSelectedValue, out var value);
            if (value == null)
            {
                return;
            }

            int number = 1;

            foreach (Dictionary<ELight, int> shot in IlluminationRecipe.Setting[CameraSelectedValue])
            {
                dynamic item = new ExpandoObject();

                ((IDictionary<string, object>)item)["ShotNumber"] = number++;

                foreach (ELight lightType in ELights)
                {
                    ((IDictionary<string, object>)item)[lightType.ToString()] = shot[lightType];
                }

                DataGridItems.Add(item);
            }

            CameraSelectedIndex = 0;
        }

        private void lightControllerPanelViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is not LightControllerPanelViewModel viewmodel)
            {
                return;
            }

            if (viewmodel.IsInterlocked && viewmodel.Brightness != 0)
            {
                foreach (LightControllerPanelViewModel l in LightControllerCollection)
                {
                    if (!l.IsInterlocked || l == viewmodel || viewmodel.InterlockGroup != l.InterlockGroup)
                    {
                        continue;
                    }

                    l.Brightness = 0;
                }
            }

            ((IDictionary<string, object>)DataGridItems[CameraSelectedIndex])[viewmodel.Light.ToString()] = viewmodel.Brightness;
            
            //this.debouncer.Debounce(100, () =>
            //{
            //    this.lightManager.SetBrightness(CameraSelectedValue, viewmodel.Light, viewmodel.Brightness);
            //});
            
            IlluminationRecipe.Setting[CameraSelectedValue][CameraSelectedIndex][viewmodel.Light] = viewmodel.Brightness;
        }

        private void addLightController()
        {
            IlluminationRecipe.Setting.TryGetValue(CameraSelectedValue, out var value);
            if (value == null)
            {
                return;
            }

            foreach (KeyValuePair<ELight, Illuminations.Light> d in this.lightManager.Lights[CameraSelectedValue])
            {
                int brightness = IlluminationRecipe.Setting[CameraSelectedValue][this.cameraSelectedIndex][d.Key];

                LightControllerPanelViewModel lightControllerPanelViewModel = new LightControllerPanelViewModel(d.Key, d.Value.Name, brightness, d.Value.MaxBrightness, d.Value.IsInterlocked, d.Value.InterlockGroup);

                lightControllerPanelViewModel.PropertyChanged += lightControllerPanelViewModel_PropertyChanged;

                LightControllerCollection.Add(lightControllerPanelViewModel);
            }
        }

        private void lightSave()
        {
            AlertWindow alertWindow = new AlertWindow("Save Light Recipe", "Would you like to save the current settings?", AlertWindow.EAlert.YESNO);
            if (alertWindow.ShowDialog() != true) return;

            this.illuminationService.SaveRecipe(IlluminationRecipe);
            this.lightManager.Save();

            new AlertWindow("Save Light Recipe", "The settings have been saved.", AlertWindow.EAlert.YES).ShowDialog();
        }

    }
}