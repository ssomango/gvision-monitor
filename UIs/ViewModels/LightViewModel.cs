using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Cameras;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GVisionWpf.Utils;

namespace GVisionWpf.UIs.ViewModels
{
    public class LightViewModel : ViewModelBase
    {
        private static readonly Lazy<LightViewModel> lazy = new Lazy<LightViewModel>(() => new LightViewModel());
        public static LightViewModel Instance => lazy.Value;

        private ObservableCollection<LightControllerPanelViewModel> lightControllerCollection = new ObservableCollection<LightControllerPanelViewModel>();
        private ObservableCollection<dynamic> dataGridItems = new ObservableCollection<dynamic>();
        private int selectedIndex;
        private ObservableCollection<DataGridColumn> columns = new ObservableCollection<DataGridColumn>();
        private ECamera camera;
        private ECamera cameraSelectedValue;

        private IlluminationService iluminationService = IlluminationService.Instance;
        private LightManager lightManager = LightManager.Instance;
        private List<ELight> eLights = new List<ELight>();

        private IlluminationRecipe illuminationRecipe;

        private Debouncer debouncer = new Debouncer(Application.Current.Dispatcher);


        #region Property

        public ICommand AddCommand { get; private set; }
        public ICommand PrsTestCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }

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

        public dynamic SelectedIndex
        {
            get => this.selectedIndex;
            set
            {
                this.selectedIndex = value;
                OnPropertyChanged();

                LightControllerCollection.Clear();
                addLightController();
                turnOnLight();
            }
        }

        public ObservableCollection<DataGridColumn> Columns
        {
            get => this.columns;
            set
            {
                this.columns = Columns;
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

        public List<Illuminations.ELight> ELights
        {
            get => this.eLights;
            set
            {
                this.eLights = value;
                OnPropertyChanged();
            }
        }

        public System.Collections.IEnumerable CameraItemsSource { get => GlobalSetting.Instance.CameraInfos.Select(c => c.CameraType); }

        public ECamera CameraSelectedValue
        {
            get => this.cameraSelectedValue;
            set
            {
                this.cameraSelectedValue = value;
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

        #endregion

        private LightViewModel()
        {
            AddCommand = new RelayCommand(addShot);
            PrsTestCommand = new RelayCommand(prsTest);
            DeleteCommand = new RelayCommand(deleteShot);
            SaveCommand = new RelayCommand(save);

            LoadRecipe();
        }

        public void LoadRecipe()
        {
            IlluminationRecipe = this.iluminationService.GetIlluminationRecipe();
        }

        public void LoadDataGridItems()
        {
            DataGridItems.Clear();

            IlluminationRecipe.Setting.TryGetValue(CameraSelectedValue, out var value);
            if (value == null) { return; }

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

            SelectedIndex = 0;
        }

        private void lightControllerPanelViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is not LightControllerPanelViewModel viewmodel) { return; }

            if (viewmodel.IsInterlocked && viewmodel.Brightness != 0)
            {
                foreach (LightControllerPanelViewModel l in LightControllerCollection)
                {
                    if (!l.IsInterlocked || l == viewmodel || viewmodel.InterlockGroup != l.InterlockGroup) { continue; }

                    l.Brightness = 0;
                }
            }

            ((IDictionary<string, object>)DataGridItems[SelectedIndex])[viewmodel.Light.ToString()] = viewmodel.Brightness;

            this.debouncer.Debounce(100, () =>
            {
                this.lightManager.SetBrightness(CameraSelectedValue, viewmodel.Light, viewmodel.Brightness);
            });

            IlluminationRecipe.Setting[CameraSelectedValue][SelectedIndex][viewmodel.Light] = viewmodel.Brightness;
        }

        private void addLightController()
        {
            IlluminationRecipe.Setting.TryGetValue(CameraSelectedValue, out var value);
            if (value == null) { return; }

            foreach (KeyValuePair<ELight, Light> d in this.lightManager.Lights[CameraSelectedValue])
            {
                int brightness = IlluminationRecipe.Setting[CameraSelectedValue][this.selectedIndex][d.Key];

                LightControllerPanelViewModel lightControllerPanelViewModel = new LightControllerPanelViewModel(d.Key, d.Value.Name, brightness, d.Value.MaxBrightness, d.Value.IsInterlocked, d.Value.InterlockGroup);

                lightControllerPanelViewModel.PropertyChanged += lightControllerPanelViewModel_PropertyChanged;

                LightControllerCollection.Add(lightControllerPanelViewModel);
            }
        }

        private void turnOnLight()
        {
            Dictionary<ELight, int> lights = new Dictionary<ELight, int>();

            foreach (ELight lightType in this.eLights)
            {
                int brightness = (int)((IDictionary<string, object>)DataGridItems[SelectedIndex])[lightType.ToString()];
                lights.Add(lightType, brightness);
            }

            this.lightManager.SetBrightness(CameraSelectedValue, lights);
        }

        private void addShot()
        {
            IlluminationRecipe.Setting.TryGetValue(CameraSelectedValue, out var value);
            if (value == null) { return; }

            Dictionary<ELight, int> shot = new Dictionary<ELight, int>();

            foreach (ELight eLight in this.eLights)
            {
                shot.Add(eLight, 0);
            }

            IlluminationRecipe.Setting[CameraSelectedValue].Add(shot);

            LoadDataGridItems();

            SelectedIndex = IlluminationRecipe.Setting[CameraSelectedValue].Count - 1;
        }

        private void prsTest()
        {
            CameraManager.Instance.Cameras[ECamera.PRS].TriggerShot();
        }

        private void deleteShot()
        {
            IlluminationRecipe.Setting.TryGetValue(CameraSelectedValue, out var value);
            if (value == null) { return; }

            if (IlluminationRecipe.Setting[CameraSelectedValue].Count < 0) { return; }

            AlertWindow alertWindow = new AlertWindow("Delete Shot", "If you delete this shot, it will change the sequence, affecting the teaching.\nAre you sure you want to delete this shot?", AlertWindow.EAlert.YESNO);
            if (alertWindow.ShowDialog() != true) return;

            IlluminationRecipe.Setting[CameraSelectedValue].RemoveAt(SelectedIndex);

            LoadDataGridItems();

            new AlertWindow("Delete Shot", "The shot has been successfully deleted.", AlertWindow.EAlert.YES).ShowDialog();
        }

        private void save()
        {
            AlertWindow alertWindow = new AlertWindow("Save Light Recipe", "Would you like to save the current settings?", AlertWindow.EAlert.YESNO);
            if (alertWindow.ShowDialog() != true) return;

            this.iluminationService.SaveRecipe(IlluminationRecipe);
            this.lightManager.Save();

            new AlertWindow("Save Light Recipe", "The settings have been saved.", AlertWindow.EAlert.YES).ShowDialog();
        }

        public void turnonlight_public()
        {
            turnOnLight();
        }
    }
}
