using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Api;
using GVisionWpf.DomainLayer.Data;
using GVisionWpf.DomainLayer.Data.Inspection.Item;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.ViewModels.Calibrations;
using GVisionWpf.UIs.ViewModels.TreeView;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;

namespace GVisionWpf.UIs.ViewModels
{

    public partial class SettingsViewModel : ViewModelBase
    {
        private static readonly Lazy<SettingsViewModel> lazy = new Lazy<SettingsViewModel>(() => new SettingsViewModel());
        public static SettingsViewModel Instance => lazy.Value;

        // TODO: 매번 함수 호출하도록 변경하기
        private bool hasMappingRecipe;
        private bool hasPrsRecipe;
        private bool hasBarCodeRecipe;
        private Device selectedDevice;
        private string selectedRecipeName;

        private TreeViewModel treeViewModel;
        private TreeNodeViewModel? selectedNode;

        private LightManager lightManager = LightManager.Instance;

        private List<DeviceViewWindow> observerDeviceViewWindows = new List<DeviceViewWindow>();

        private List<string> recipeNames = new List<string>();

        public void AddDeviceViewWindow(DeviceViewWindow deviceViewWindow)
        {
            this.observerDeviceViewWindows.Add(deviceViewWindow);
        }

        #region Property

        [ObservableProperty]
        private Visibility moldInspectionVisibility = GlobalSetting.Instance.MoldInspectionVisibility;

        [ObservableProperty]
        private Visibility bgaInspectionVisibility = GlobalSetting.Instance.BgaInspectionVisibility;

        [ObservableProperty]
        private Visibility lgaInspectionVisibility = GlobalSetting.Instance.LgaInspectionVisibility;

        [ObservableProperty]
        private Visibility qfnInspectionVisibility = GlobalSetting.Instance.QfnInspectionVisibility;


        #region Recipe

        public ICommand SaveCommand { get; private set; }
        public ICommand UseMappingCommand { get; private set; }
        public ICommand UsePrsCommand { get; private set; }
        public ICommand UseBarCodeCommand { get; private set; }
        public ICommand AddRecipeCommand { get; private set; }
        public ICommand CopyRecipeCommand { get; private set; }
        public ICommand RenameRecipeCommand { get; private set; }
        public ICommand DeleteRecipeCommand { get; private set; }
        public ICommand SelectRecipeCommand { get; private set; }

        

        public EInspection PrsPackageType
        {
            get => this.selectedDevice.PrsPackageType;
            set
            {
                this.selectedDevice.PrsPackageType = value;
                GVisionMessenger.Instance.Recipe.SendChangedPrsType(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPrsRecipe));
            }
        }

        public EInspection MapPackageType
        {
            get => this.selectedDevice.MapPackageType;
            set
            {
                this.selectedDevice.MapPackageType = value;
                GVisionMessenger.Instance.Recipe.SendChangedMappingType(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(hasMappingRecipe));
            }
        }

        public List<EInspection> PackageTypes { get; } = new List<EInspection>
        {
            EInspection.Bga, EInspection.Lga, EInspection.Qfn, EInspection.Mark,
        };

        public List<EColor> ResultColors { get; } = Enum.GetValues<EColor>().ToList();

        public string SelectedRecipeName
        {
            get => this.selectedRecipeName;
            set
            {
                this.selectedRecipeName = value;
                applyRecipe(value);
                checkTeachingFiles();
                OnPropertyChanged();
            }
        }

        public Device SelectedDevice
        {
            get => this.selectedDevice;
            set
            {
                this.selectedDevice = value;
                TrayRowCount = value.TraySize.Row;
                TrayColCount = value.TraySize.Col;
                FovRowCount = value.FovSize.Row;
                FovColCount = value.FovSize.Col;
                BlockRowCount = value.BlockSize.Row;
                BlockColCount = value.BlockSize.Col;
                PackageHeight = value.PackageSize.Height;
                PackageWidth = value.PackageSize.Width;

                PrsPackageType = value.PrsPackageType;
                MapPackageType = value.MapPackageType;

                PrsPackageType = value.PrsPackageType;
                MapPackageType = value.MapPackageType;
                IsMappingUsed = value.IsMappingUsed;
                IsPrsUsed = value.IsPrsUsed;
                IsBarcodeUsed = value.IsBarcodeUsed;
                OnPropertyChanged();
            }
        }

        public TreeViewModel TreeViewModel
        {
            get => this.treeViewModel;
            set
            {
                this.treeViewModel = value;
                OnPropertyChanged();
            }
        }

        public TreeNodeViewModel? SelectedNode
        {
            get => this.selectedNode;
            set
            {
                this.selectedNode = value;
                OnPropertyChanged();
            }
        }

        public int TrayRowCount
        {
            get => SelectedDevice.TraySize.Row;
            set
            {
                SelectedDevice.TraySize.Row = value;
                OnPropertyChanged();
            }
        }

        public int TrayColCount
        {
            get => SelectedDevice.TraySize.Col;
            set
            {
                SelectedDevice.TraySize.Col = value;
                OnPropertyChanged();
            }
        }

        public int FovRowCount
        {
            get => SelectedDevice.FovSize.Row;
            set
            {
                SelectedDevice.FovSize.Row = value;
                OnPropertyChanged();
            }
        }

        public int FovColCount
        {
            get => SelectedDevice.FovSize.Col;
            set
            {
                SelectedDevice.FovSize.Col = value;
                OnPropertyChanged();
            }
        }

        public int BlockRowCount
        {
            get => SelectedDevice.BlockSize.Row;
            set
            {
                SelectedDevice.BlockSize.Row = value;
                OnPropertyChanged();
            }
        }

        public int BlockColCount
        {
            get => SelectedDevice.BlockSize.Col;
            set
            {
                SelectedDevice.BlockSize.Col = value;
                OnPropertyChanged();
            }
        }

        public double PackageWidth
        {
            get => SelectedDevice.PackageSize.Width;
            set
            {
                SelectedDevice.PackageSize.Width = value;
                OnPropertyChanged();
            }
        }

        public double PackageHeight
        {
            get => SelectedDevice.PackageSize.Height;
            set
            {
                SelectedDevice.PackageSize.Height = value;
                OnPropertyChanged();
            }
        }

        public bool IsMappingUsed
        {
            get => SelectedDevice.IsMappingUsed;
            set
            {
                SelectedDevice.IsMappingUsed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseMappingBackground));
            }
        }

        public bool HasMappingRecipe
        {
            get => this.hasMappingRecipe;
            set
            {
                this.hasMappingRecipe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMappingRecipeBackground));
            }
        }

        public bool IsPrsUsed
        {
            get => SelectedDevice.IsPrsUsed;
            set
            {
                SelectedDevice.IsPrsUsed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsePrsBackground));
            }
        }

        public bool HasPrsRecipe
        {
            get => this.hasPrsRecipe;
            set
            {
                this.hasPrsRecipe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPrsRecipeBackground));
            }
        }

        public bool IsBarcodeUsed
        {
            get => SelectedDevice.IsBarcodeUsed;
            set
            {
                SelectedDevice.IsBarcodeUsed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseBarCodeBackground));
            }
        }

        public bool HasBarCodeRecipe
        {
            get => this.hasBarCodeRecipe;
            set
            {
                this.hasBarCodeRecipe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasBarCodeRecipeBackground));
            }
        }

        public SolidColorBrush UseMappingBackground => IsMappingUsed ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        public SolidColorBrush UsePrsBackground => IsPrsUsed ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        public SolidColorBrush UseBarCodeBackground => IsBarcodeUsed ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        public SolidColorBrush HasMappingRecipeBackground => HasMappingRecipe ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        public SolidColorBrush HasPrsRecipeBackground => HasPrsRecipe ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        public SolidColorBrush HasBarCodeRecipeBackground => HasBarCodeRecipe ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        private void setUseItem<T>(HashSet<T> inspectionItems, T item, bool value) where T : InspectionItem
        {
            if (value)
            {
                inspectionItems.Add(item);
            }
            else
            {
                inspectionItems.Remove(item);
            }
            OnPropertyChanged();
        }

        private bool getUseItem<T>(HashSet<T> inspectionItems, T item) where T : InspectionItem
        {
            return inspectionItems.Contains(item);
        }

        private void setItemColor(Dictionary<EResultType, EColor> inspectionColors, EResultType item, EColor value)
        {
            inspectionColors[item] = value;
            OnPropertyChanged();
        }

        private EColor getItemColor(Dictionary<EResultType, EColor> inspectionColors, EResultType item)
        {
            return inspectionColors[item];
        }

        public List<string> RecipeNames
        {
            get => this.recipeNames;
            set
            {
                this.recipeNames = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region BGAInspection
        public bool UseBgaNoDevice
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.NoDevice);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.NoDevice, value);
        }

        public EColor BgaNoDeviceColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.NoDevice);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.NoDevice, value);
        }

        public bool UseBgaPackageSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.PackageSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.PackageSize, value);
        }

        public EColor BgaPackageSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.PackageSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.PackageSize, value);
        }

        public bool UseBgaPackageOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.PackageOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.PackageOffset, value);
        }

        public EColor BgaPackageOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.PackageOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.PackageOffset, value);
        }

        public bool UseBgaCornerDegree
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.CornerDegree);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.CornerDegree, value);
        }

        public EColor BgaCornerDegreeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.CornerDegree);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.CornerDegree, value);
        }

        public bool UseBgaFirstPin
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.FirstPin);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.FirstPin, value);
        }

        public EColor BgaFirstPinColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.FirstPin);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.FirstPin, value);
        }

        public bool UseBgaPattern
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Pattern);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Pattern, value);
        }

        public EColor BgaPatternColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Pattern);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Pattern, value);
        }

        public bool UseBgaBallCount
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallCount);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallCount, value);
        }

        public EColor BgaBallCountColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallCount);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallCount, value);
        }

        public bool UseBgaBallSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallSize, value);
        }

        public EColor BgaBallSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallSize, value);
        }

        public bool UseBgaBallPitch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallPitch);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallPitch, value);
        }

        public EColor BgaBallPitchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallPitch);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallPitch, value);
        }

        public bool UseBgaBallBridging
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallBridging);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallBridging, value);
        }

        public EColor BgaBallBridgingColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallBridging);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallBridging, value);
        }

        public bool UseBgaExtraBall
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.ExtraBall);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.ExtraBall, value);
        }

        public EColor BgaExtraBallColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.ExtraBall);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.ExtraBall, value);
        }

        public bool UseBgaMissingBall
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.MissingBall);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.MissingBall, value);
        }

        public EColor BgaMissingBallColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.MissingBall);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.MissingBall, value);
        }

        public bool UseBgaCrackBall
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.CrackBall);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.CrackBall, value);
        }

        public EColor BgaCrackBallColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.CrackBall);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.CrackBall, value);
        }

        public bool UseBgaScratch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Scratch);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Scratch, value);
        }

        public EColor BgaScratchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Scratch);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Scratch, value);
        }

        public bool UseBgaForeignMaterial
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.ForeignMaterial);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.ForeignMaterial, value);
        }

        public EColor BgaForeignMaterialColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.ForeignMaterial);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.ForeignMaterial, value);
        }

        public bool UseBgaContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Contamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Contamination, value);
        }

        public EColor BgaContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Contamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Contamination, value);
        }

        public bool UseBallPosition
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallPosition);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.BallPosition, value);
        }

        public EColor BgaBallPositionColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallPosition);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.BallPosition, value);
        }

        public bool UseBgaSawOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.SawOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.SawOffset, value);
        }

        public EColor BgaSawOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.SawOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.SawOffset, value);
        }

        public bool UseBgaChipping
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Chipping);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Chipping, value);
        }

        public EColor BgaChippingColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Chipping);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Chipping, value);
        }

        public bool UseBgaBurr
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Burr);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.Burr, value);
        }

        public EColor BgaBurrColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Burr);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.Burr, value);
        }

        public bool UseBgaRejectMark
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.RejectMark);
            set => setUseItem(GlobalSetting.Instance.Inspection.BgaItems, BgaInspectionItem.RejectMark, value);
        }

        public EColor BgaRejectMarkColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.RejectMark);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.RejectMark, value);
        }

        public EColor BgaXOutColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.XOut);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.XOut, value);
        }

        public EColor BgaXOut2Color
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.XOut2);
            set => setItemColor(GlobalSetting.Instance.Inspection.BgaColors, EResultType.XOut2, value);
        }

        #endregion

        #region QFNInspection

        public bool UseQfnNoDevice
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.NoDevice);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.NoDevice, value);
        }

        public EColor QfnNoDeviceColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.NoDevice);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.NoDevice, value);
        }

        public bool UseQfnPackageSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PackageSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PackageSize, value);
        }

        public EColor QfnPackageSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PackageSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PackageSize, value);
        }

        public bool UseQfnPackageOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PackageOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PackageOffset, value);
        }

        public EColor QfnPackageOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PackageOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PackageOffset, value);
        }

        public bool UseQfnCornerDegree
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.CornerDegree);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.CornerDegree, value);
        }

        public EColor QfnCornerDegreeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.CornerDegree);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.CornerDegree, value);
        }

        public bool UseQfnFirstPin
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.FirstPin);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.FirstPin, value);
        }

        public EColor QfnFirstPinColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.FirstPin);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.FirstPin, value);
        }

        public bool UseQfnPadSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PadSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PadSize, value);
        }

        public EColor QfnPadSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PadSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PadSize, value);
        }

        public bool UseQfnPadArea
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PadArea);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.PadArea, value);
        }

        public EColor QfnPadAreaColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PadArea);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.PadArea, value);
        }

        public bool UseQfnLeadCount
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadCount);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadCount, value);
        }

        public EColor QfnLeadCountColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadCount);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadCount, value);
        }

        public bool UseQfnLeadSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadSize, value);
        }

        public EColor QfnLeadSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadSize, value);
        }

        public bool UseQfnLeadPitch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadPitch);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadPitch, value);
        }

        public EColor QfnLeadPitchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadPitch);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadPitch, value);
        }

        public bool UseQfnLeadOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadOffset, value);
        }

        public EColor QfnLeadOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadOffset, value);
        }

        public bool UseQfnLeadArea
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadArea);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadArea, value);
        }

        public EColor QfnLeadAreaColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadArea);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadArea, value);
        }

        public bool UseQfnLeadContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadContamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadContamination, value);
        }

        public EColor QfnLeadContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadContamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadContamination, value);
        }

        public bool UseQfnLeadPerimeter
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadPerimeter);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.LeadPerimeter, value);
        }

        public EColor QfnLeadPerimeterColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadPerimeter);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.LeadPerimeter, value);
        }

        public bool UseQfnScratch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Scratch);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Scratch, value);
        }

        public EColor QfnScratchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Scratch);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Scratch, value);
        }

        public bool UseQfnForeignMaterial
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.ForeignMaterial);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.ForeignMaterial, value);
        }

        public EColor QfnForeignMaterialColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.ForeignMaterial);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.ForeignMaterial, value);
        }

        public bool UseQfnContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Contamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Contamination, value);
        }

        public EColor QfnContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Contamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Contamination, value);
        }

        public bool UseQfnSawOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.SawOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.SawOffset, value);
        }

        public EColor QfnSawOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.SawOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.SawOffset, value);
        }

        public bool UseQfnChipping
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Chipping);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Chipping, value);
        }

        public EColor QfnChippingColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Chipping);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Chipping, value);
        }

        public bool UseQfnBurr
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Burr);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.Burr, value);
        }

        public EColor QfnBurrColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Burr);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.Burr, value);
        }

        public bool UseQfnRejectMark
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.RejectMark);
            set => setUseItem(GlobalSetting.Instance.Inspection.QfnItems, QfnInspectionItem.RejectMark, value);
        }

        public EColor QfnRejectMarkColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.RejectMark);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.RejectMark, value);
        }

        public EColor QfnXOutColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.XOut);
            set => setItemColor(GlobalSetting.Instance.Inspection.QfnColors, EResultType.XOut, value);
        }

        #endregion

        #region MAPInspection
        public bool UseMapNoDevice
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.NoDevice);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.NoDevice, value);
        }

        public EColor MapNoDeviceColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.NoDevice);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.NoDevice, value);
        }

        public bool UseMapPackageSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.PackageSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.PackageSize, value);
        }

        public EColor MapPackageSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.PackageSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.PackageSize, value);
        }

        public bool UseMapPackageOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.PackageOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.PackageOffset, value);
        }

        public EColor MapPackageOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.PackageOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.PackageOffset, value);
        }

        public bool UseMapCornerDegree
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.CornerDegree);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.CornerDegree, value);
        }

        public EColor MapCornerDegreeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.CornerDegree);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.CornerDegree, value);
        }

        public bool UseMapNoMark
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.NoMark);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.NoMark, value);
        }

        public EColor MapNoMarkColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.NoMark);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.NoMark, value);
        }

        public bool UseMapMarkCount
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.MarkCount);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.MarkCount, value);
        }

        public EColor MapMarkCountColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.MarkCount);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.MarkCount, value);
        }

        public bool UseMapWrongMark
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.WrongMark);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.WrongMark, value);
        }

        public EColor MapWrongMarkColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.WrongMark);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.WrongMark, value);
        }

        public bool UseMapTextAngle
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.TextAngle);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.TextAngle, value);
        }

        public EColor MapTextAngleColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.TextAngle);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.TextAngle, value);
        }

        public bool UseMapTextOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.TextOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.TextOffset, value);
        }

        public EColor MapTextOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.TextOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.TextOffset, value);
        }

        public bool UseMapDataCode
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.DataCode);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.DataCode, value);
        }

        public EColor MapDataCodeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.DataCode);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.DataCode, value);
        }

        public bool UseMapMissingChar
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.MissingChar);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.MissingChar, value);
        }

        public EColor MapMissingCharColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.MissingChar);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.MissingChar, value);
        }

        public bool UseMapScratch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Scratch);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Scratch, value);
        }

        public EColor MapScratchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Scratch);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Scratch, value);
        }

        public bool UseMapForeignMaterial
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.ForeignMaterial);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.ForeignMaterial, value);
        }

        public EColor MapForeignMaterialColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.ForeignMaterial);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.ForeignMaterial, value);
        }

        public bool UseMapContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Contamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Contamination, value);
        }

        public EColor MapContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Contamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Contamination, value);
        }

        public bool UseMapSawOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.SawOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.SawOffset, value);
        }

        public EColor MappingSawOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.SawOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.SawOffset, value);
        }

        public bool UseMapChipping
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Chipping);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Chipping, value);
        }

        public EColor MapChippingColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Chipping);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Chipping, value);
        }

        public bool UseMapBurr
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Burr);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.Burr, value);
        }

        public EColor MapBurrColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Burr);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.Burr, value);
        }

        public bool UseMapRejectMark
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.RejectMark);
            set => setUseItem(GlobalSetting.Instance.Inspection.MoldItems, MoldInspectionItem.RejectMark, value);
        }

        public EColor MapRejectMarkColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.RejectMark);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.RejectMark, value);
        }

        public EColor MapXOutColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.XOut);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.XOut, value);
        }

        public EColor MapXOut2Color
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.XOut2);
            set => setItemColor(GlobalSetting.Instance.Inspection.MapColors, EResultType.XOut2, value);
        }

        #endregion

        #region LGAInspection

        public bool UseLgaNoDevice
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.NoDevice);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.NoDevice, value);
        }

        public EColor LgaNoDeviceColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.NoDevice);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.NoDevice, value);
        }

        public bool UseLgaPackageSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.PackageSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.PackageSize, value);
        }

        public EColor LgaPackageSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.PackageSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.PackageSize, value);
        }

        public bool UseLgaPackageOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.PackageOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.PackageOffset, value);
        }

        public EColor LgaPackageOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.PackageOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.PackageOffset, value);
        }

        public bool UseLgaCornerDegree
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.CornerDegree);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.CornerDegree, value);
        }

        public EColor LgaCornerDegreeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.CornerDegree);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.CornerDegree, value);
        }

        public bool UseLgaFirstPin
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.FirstPin);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.FirstPin, value);
        }

        public EColor LgaFirstPinColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.FirstPin);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.FirstPin, value);
        }

        public bool UseLgaPadCount
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadCount);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadCount, value);
        }

        public EColor LgaPadCountColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadCount);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadCount, value);
        }

        public bool UseLgaPadSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadSize, value);
        }

        public EColor LgaPadSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadSize, value);
        }

        public bool UseLgaPadPitch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadPitch);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadPitch, value);
        }

        public EColor LgaPadPitchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadPitch);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadPitch, value);
        }

        public bool UseLgaPadOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadOffset, value);
        }

        public EColor LgaPadOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadOffset, value);
        }

        public bool UseLgaPadArea
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadArea);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadArea, value);
        }

        public EColor LgaPadAreaColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadArea);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadArea, value);
        }

        public bool UseLgaPadContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadContamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadContamination, value);
        }

        public EColor LgaPadContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadContamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadContamination, value);
        }

        public bool UseLgaPadPerimeter
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadPerimeter);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.MultiPadPerimeter, value);
        }

        public EColor LgaPadPerimeterColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadPerimeter);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.MultiPadPerimeter, value);
        }

        public bool UseLgaLeadCount
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadCount);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadCount, value);
        }

        public EColor LgaLeadCountColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadCount);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadCount, value);
        }

        public bool UseLgaLeadSize
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadSize);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadSize, value);
        }

        public EColor LgaLeadSizeColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadSize);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadSize, value);
        }

        public bool UseLgaLeadPitch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadPitch);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadPitch, value);
        }

        public EColor LgaLeadPitchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadPitch);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadPitch, value);
        }

        public bool UseLgaLeadOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadOffset, value);
        }

        public EColor LgaLeadOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadOffset, value);
        }

        public bool UseLgaLeadArea
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadArea);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadArea, value);
        }

        public EColor LgaLeadAreaColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadArea);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadArea, value);
        }

        public bool UseLgaLeadContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadContamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadContamination, value);
        }

        public EColor LgaLeadContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadContamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadContamination, value);
        }

        public bool UseLgaLeadPerimeter
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadPerimeter);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.LeadPerimeter, value);
        }

        public EColor LgaLeadPerimeterColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadPerimeter);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.LeadPerimeter, value);
        }

        public bool UseLgaScratch
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Scratch);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Scratch, value);
        }

        public EColor LgaScratchColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Scratch);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Scratch, value);
        }

        public bool UseLgaForeignMaterial
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.ForeignMaterial);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.ForeignMaterial, value);
        }

        public EColor LgaForeignMaterialColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.ForeignMaterial);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.ForeignMaterial, value);
        }

        public bool UseLgaContamination
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Contamination);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Contamination, value);
        }

        public EColor LgaContaminationColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Contamination);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Contamination, value);
        }

        public double LgaSawOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetX;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetX, value);
        }

        public double LgaSawOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetY;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaSawOffsetY, value);
        }

        public bool UseLgaSawOffset
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.SawOffset);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.SawOffset, value);
        }

        public EColor LgaSawOffsetColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.SawOffset);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.SawOffset, value);
        }

        public bool UseLgaChipping
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Chipping);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Chipping, value);
        }

        public EColor LgaChippingColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Chipping);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Chipping, value);
        }

        public bool UseLgaBurr
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Burr);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.Burr, value);
        }

        public EColor LgaBurrColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Burr);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.Burr, value);
        }

        public bool UseLgaRejectMark
        {
            get => getUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.RejectMark);
            set => setUseItem(GlobalSetting.Instance.Inspection.LgaItems, LgaInspectionItem.RejectMark, value);
        }

        public EColor LgaRejectMarkColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.RejectMark);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.RejectMark, value);
        }

        #endregion
        public EColor LgaXOutColor
        {
            get => getItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.XOut);
            set => setItemColor(GlobalSetting.Instance.Inspection.LgaColors, EResultType.XOut, value);
        }

        #endregion

        #region Tolerance

        public double BgaPackageSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaPackageSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaPackageSize.Width, value);
        }

        public double BgaPackageSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaPackageSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaPackageSize.Height, value);
        }

        public double BgaCornerDegree
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaCornerDegree;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaCornerDegree, value);
        }

        public double BgaSawOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetX, value);
        }

        public double BgaSawOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetY;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaSawOffsetY, value);
        }


        public double BgaSawOffsetXStandard
        {
            get => GlobalSetting.Instance.Inspection.BgaSawOffsetXStandard;
            set => SetField(ref GlobalSetting.Instance.Inspection.BgaSawOffsetXStandard, value);
        }

        public double BgaSawOffsetYStandard
        {
            get => GlobalSetting.Instance.Inspection.BgaSawOffsetYStandard;
            set => SetField(ref GlobalSetting.Instance.Inspection.BgaSawOffsetYStandard, value);
        }

        public double BgaBallSizeDiameter
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaBallSizeDiameter;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaBallSizeDiameter, value);
        }

        public double BgaBallSizeDiameterStandard
        {
            get => GlobalSetting.Instance.Inspection.BallDiameters;
            set => SetField(ref GlobalSetting.Instance.Inspection.BallDiameters, value);
        }

        public double BgaBallPitch
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.BgaBallPitch;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.BgaBallPitch, value);
        }

        public double BgaBallPitchStandard
        {
            get => GlobalSetting.Instance.Inspection.BallPitch;
            set => SetField(ref GlobalSetting.Instance.Inspection.BallPitch, value);
        }

        public double QfnPackageSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnPackageSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnPackageSize.Width, value);
        }

        public double QfnPackageSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnPackageSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnPackageSize.Height, value);
        }

        public double QfnCornerDegree
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnCornerDegree;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnCornerDegree, value);
        }

        public double QfnSawOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetX;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetX, value);
        }

        public double QfnSawOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetY;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnSawOffsetY, value);
        }

        public double QfnPadSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnPadSizeWidth;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnPadSizeWidth, value);
        }

        public double QfnPadSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnPadSizeHeight;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnPadSizeHeight, value);
        }

        public int QfnPadArea
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnPadArea;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnPadArea, value);
        }

        public double QfnLeadSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadSize.Width, value);
        }

        public double QfnLeadSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadSize.Height, value);
        }

        public int QfnLeadArea
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadArea;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadArea, value);
        }

        public double QfnLeadPitch
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadPitch;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadPitch, value);
        }

        public double QfnLeadOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset.X;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset.X, value);
        }

        public double QfnLeadOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset.Y;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset.Y, value);
        }

        public double QfnLeadOffsetT
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset.T;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadOffset.T, value);
        }

        public double QfnLeadPerimeter
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.QfnLeadPerimeter;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.QfnLeadPerimeter, value);
        }

        public double MapPackageSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize.Width, value);
        }

        public double MapPackageSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapPackageSize.Height, value);
        }

        public double MappingSawOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetY;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetY, value);
        }

        public double MappingSawOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapSawOffsetX, value);
        }

        public int MarkCount
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MarkCount;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MarkCount, value);
        }

        public double MapTextOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetX;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetX, value);
        }

        public double MapTextOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetY;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetY, value);
        }

        public double MapTextOffsetT
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetT;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapTextOffsetT, value);
        }

        public double MapCornerDegree
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.MapCornerDegree;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.MapCornerDegree, value);
        }

        public double LgaPackageSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPackageSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPackageSize.Width, value);
        }

        public double LgaPackageSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPackageSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPackageSize.Height, value);
        }

        public double LgaCornerDegree
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaCornerDegree;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaCornerDegree, value);
        }

        public double LgaPadSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadSize.Width, value);
        }

        public double LgaPadSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadSize.Height, value);
        }

        public int LgaPadArea
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadArea;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadArea, value);
        }

        public double LgaPadPitch
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadPitch;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadPitch, value);
        }

        public double LgaPadOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset.X;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset.X, value);
        }

        public double LgaPadOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset.Y;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset.Y, value);
        }

        public double LgaPadOffsetT
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset.T;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadOffset.T, value);
        }

        public double LgaPadPerimeter
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaPadPerimeter;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaPadPerimeter, value);
        }

        public double LgaLeadSizeWidth
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadSize.Width;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadSize.Width, value);
        }

        public double LgaLeadSizeHeight
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadSize.Height;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadSize.Height, value);
        }

        public int LgaLeadArea
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadArea;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadArea, value);
        }

        public double LgaLeadPitch
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadPitch;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadPitch, value);
        }

        public double LgaLeadOffsetX
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset.X;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset.X, value);
        }

        public double LgaLeadOffsetY
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset.Y;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset.Y, value);
        }

        public double LgaLeadOffsetT
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset.T;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadOffset.T, value);
        }

        public double LgaLeadPerimeter
        {
            get => GlobalSetting.Instance.Inspection.Tolerance.LgaLeadPerimeter;
            set => SetField(ref GlobalSetting.Instance.Inspection.Tolerance.LgaLeadPerimeter, value);
        }

        #endregion

        #region ETC

        public ESaveOption SaveOption
        {
            get => GlobalSetting.Instance.Inspection.SaveOption;
            set
            {
                CurrentSettingViewmodel.Instance.SaveOption = value;
                SetField(ref GlobalSetting.Instance.Inspection.SaveOption, value);
            }
        }

        public int SaveDays
        {
            get => GlobalSetting.Instance.Inspection.SaveDays;
            set => SetField(ref GlobalSetting.Instance.Inspection.SaveDays, value);
        }

        public int DBSaveDays
        {
            get => GlobalSetting.Instance.Inspection.DBSaveDays;
            set => SetField(ref GlobalSetting.Instance.Inspection.DBSaveDays, value);
        }

        public List<EInspectionMode> InpectionModes { get; } = new List<EInspectionMode>
        {
            EInspectionMode.Normal, EInspectionMode.AllPass
        };

        public EInspectionMode InpectionModeSelectedItem
        {
            get => GlobalSetting.Instance.Inspection.Mode;
            set
            {
                GlobalSetting.Instance.Inspection.Mode = value;
                CurrentSettingViewmodel.Instance.InspectionMode = value;
                OnPropertyChanged();
            }
        }

        public string Symbol
        {
            get => GlobalSetting.Instance.Inspection.LengthUnit.Symbol;
        }

        #endregion

        public SettingsViewModel()
        {
            TreeViewModel = new TreeViewModel();
            getAllDirectoriesFromPath("DB/Recipes/");
            SelectedNode = TreeViewModel.Tree.Root.Children[0];

            SaveCommand = new RelayCommand(save);
            UseMappingCommand = new RelayCommand(toggleMappingInspection);
            UsePrsCommand = new RelayCommand(togglePrsInspection);
            UseBarCodeCommand = new RelayCommand(toggleBarCodeInspection);
            AddRecipeCommand = new RelayCommand(addRecipe);
            CopyRecipeCommand = new RelayCommand(copyRecipe);
            RenameRecipeCommand = new RelayCommand(renameRecipe);
            DeleteRecipeCommand = new RelayCommand(deleteRecipe);
            SelectRecipeCommand = new RelayCommand(selectRecipe);

            this.selectedRecipeName = GlobalSetting.Instance.DeviceInfo.RecipeName;

            checkTeachingFiles();

            try
            {
                this.selectedDevice = DeviceRecipeRepository.Instance.GetRecipe();
            }
            catch
            {
                this.selectedDevice = new Device();
                GVisionMessenger.Instance.UI.SendSystemInfoMessage("저장된 레시피가 없습니다.");
            }
        }

        public void RefreshRecipeList()
        {
            string? previouslySelected = SelectedRecipeName;

            TreeViewModel = new TreeViewModel();
            getAllDirectoriesFromPath("DB/Recipes/");

            if (previouslySelected != null)
            {
                SelectedNode = TreeViewModel.Tree.Root.FindNodeByName(previouslySelected);
            }
            
            if (SelectedNode == null)
            {
                SelectedNode = TreeViewModel.Tree.Root.Children.FirstOrDefault()?.Children.FirstOrDefault();
            }
        }

        private void addRecipe()
        {
            if (SelectedNode == null) return;

            InputTextWindow inputTextWindow = new InputTextWindow("Input New Recipe", "New Name");

            if (inputTextWindow.ShowDialog() != true) return;


            string newRecipePath = "DB/Recipes" + getAllPath(SelectedNode);
            string newRecipeName = inputTextWindow.xTextBox.Text;

            if (string.IsNullOrEmpty(newRecipeName)) throw new WrongDirectoryException();

            if (this.recipeNames.Contains(newRecipeName))
            {
                new AlertWindow("Exist Name", AlertWindow.EIcon.ALERT, "It is an already existing recipe name.", AlertWindow.EAlert.YES).ShowDialog();
                return;
            }

            string path = Path.Combine(Directory.GetCurrentDirectory(), newRecipePath + newRecipeName);

            // if (Directory.Exists(path)) throw new DirectoryDuplicatedException();

            Directory.CreateDirectory(path);
            saveRecipe(path);

            GVisionMessenger.Instance.UI.SendSystemInfoMessage(newRecipeName + "의 레시피가 생성되었습니다.");


            TreeViewModel = new TreeViewModel();
            getAllDirectoriesFromPath("DB/Recipes/");
            SelectedNode = TreeViewModel.Tree.Root.Children[0].Children[0];
        }

        private void saveRecipe(string path)
        {
            Device defaultDevice = new Device();
            SelectedDevice = defaultDevice;
            DeviceRecipeRepository.Instance.SaveRecipeByPath(defaultDevice, path);

            BgaRepository.Instance.SaveRecipeByPath(new BgaTeaching { IsTaught = false }, path);
            QfnRepository.Instance.SaveRecipeByPath(new QfnTeaching { IsTaught = false }, path);
            MoldRepository.Instance.SaveRecipeByPath(new MoldTeaching { IsTaught = false }, path);
            LgaRepository.Instance.SaveRecipeByPath(new LgaTeaching { IsTaught = false }, path);

            GridMoldRepository.Instance.SaveRecipeByPath(new GridMoldTeaching { IsTaught = false }, path);
            GridBgaRepository.Instance.SaveRecipeByPath(new GridBgaTeaching { IsTaught = false }, path);
            GridLgaRepository.Instance.SaveRecipeByPath(new GridLgaTeaching { IsTaught = false }, path);
            GridQfnRepository.Instance.SaveRecipeByPath(new GridQfnTeaching { IsTaught = false }, path);

            StripRepository.Instance.SaveRecipeByPath(new StripTeaching { IsTaught = false }, path);
            IlluminationRepository.Instance.SaveRecipeByPath(new IlluminationRecipe { IsTaught = false }, path);

            IlluminationRecipe illuminationRecipe = new IlluminationRecipe { IsTaught = false };
            foreach (KeyValuePair<ECamera, Dictionary<ELight, Light>> lightSettings in this.lightManager.Lights)
            {
                illuminationRecipe.Setting.Add(lightSettings.Key, new List<Dictionary<ELight, int>>());

                Dictionary<ELight, Light> lightByCamera = lightSettings.Value;
                List<Dictionary<ELight, int>> defaultLightValues = new List<Dictionary<ELight, int>>();
//                Dictionary<ELight, int> lightValues = new Dictionary<ELight, int>();

                foreach (KeyValuePair<ELight, Light> light in lightByCamera)
                {
                    illuminationRecipe.Setting[lightSettings.Key][0].Add(light.Key, 0);
                }
            }

            IlluminationRepository.Instance.SaveRecipeByPath(illuminationRecipe, path);
        }

        private void copyRecipe()
        {
            if (SelectedNode == null) return;
            if (SelectedNode.Data.Name == "Root" || SelectedNode.Data.Name == "Recipes") return;

            string selectedRecipePath = "DB/Recipes" + getAllPath(SelectedNode);
            string newRecipePath = "DB/Recipes" + getParentPath(SelectedNode) + " (copy)";
            string newRecipeName = SelectedNode.Data.Name + " (copy)";

            if (this.recipeNames.Contains(newRecipeName))
            {
                AlertWindow nameAlertWindow = new AlertWindow("Existing Recipe", "A copy of this recipe already exists. Proceeding with the copy will overwrite the existing recipe. Are you sure you want to copy this recipe?", AlertWindow.EAlert.YESNO);
                if (nameAlertWindow.ShowDialog() != true) return;
            }
            else
            {
                AlertWindow alertWindow = new AlertWindow("Copy Recipe", "Are you sure you want to copy this recipe?", AlertWindow.EAlert.YESNO);
                if (alertWindow.ShowDialog() != true) return;
            }

            copyDirectory(selectedRecipePath, newRecipePath);

            GVisionMessenger.Instance.UI.SendSystemInfoMessage(newRecipeName + "의 레시피가 생성되었습니다.");


            TreeViewModel = new TreeViewModel();
            getAllDirectoriesFromPath("DB/Recipes/");
            SelectedNode = TreeViewModel.Tree.Root.Children[0].Children[0];
        }

        private void copyDirectory(string sourceFolder, string destFolder)
        {
            try
            {
                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }

                string[] files = Directory.GetFiles(sourceFolder);
                string[] folders = Directory.GetDirectories(sourceFolder);

                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true);
                }

                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(destFolder, name);
                    copyDirectory(folder, dest);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void save()
        {
            AlertWindow alertWindow = new AlertWindow("Save Recipe", "Would you like to save the current settings?", AlertWindow.EAlert.YESNO);
            if (alertWindow.ShowDialog() != true) return;

            DeviceRecipeRepository.Instance.SaveRecipe(SelectedDevice);

            applyRecipe(SelectedRecipeName);

            var differences = GlobalSetting.GetDiffOfPreviousVersion();
            HistoryService.Instance.CreateHistory("Setting", differences);

            GlobalSetting.Instance.Persist();

            if (this.observerDeviceViewWindows.Count > 0)
            {
                new AlertWindow("Save Recipe", "The settings have been saved.", AlertWindow.EAlert.YES, this.observerDeviceViewWindows).ShowDialog();
            }
            else
            {
                new AlertWindow("Save Recipe", "The settings have been saved.", AlertWindow.EAlert.YES).ShowDialog();
            }
        }

        private void applyRecipe(string recipeName)
        {
            GlobalSetting.Instance.DeviceInfo.RecipeName = recipeName;
            GlobalSetting.Instance.ApplySetting();

            Device currentDevice = DeviceRecipeRepository.Instance.GetRecipe();
            SelectedDevice = currentDevice;

            if (File.Exists(GlobalSetting.Instance.DeviceInfo.VisionCurrentRecipePath))
            {
                string title = "[RECIPE]";
                string currentRecipe = $"name={recipeName.Trim()}";
                File.WriteAllText(GlobalSetting.Instance.DeviceInfo.VisionCurrentRecipePath, $"{title}\n{currentRecipe}");
            }
            GVisionMessenger.Instance.UI.SendSystemInfoMessage(SelectedRecipeName + "의 레시피로 설정을 변경하였습니다.");
        }

        private void deleteRecipe()
        {
            if (SelectedNode == null) return;
            if (SelectedNode.Data.Name == "Root" || SelectedNode.Data.Name == "Recipes") return;
            if (SelectedNode == TreeViewModel.Tree.Root.Children[0].Children[0] && TreeViewModel.Tree.Root.Children[0].Children.Count <= 1) return;

            string settingRecipeName = GlobalSetting.Instance.DeviceInfo.RecipeName;
            if (SelectedNode.Data.Name == settingRecipeName)
            {
                new AlertWindow("Alert", AlertWindow.EIcon.ALERT, "The currently selected recipe cannot be deleted.", AlertWindow.EAlert.YES).ShowDialog();
                return;
            }

            var node = TreeViewModel.Tree.Root.FindNodeByName(settingRecipeName);
            if (SelectedNode.ContainsNode(node))
            {
                new AlertWindow("Alert", AlertWindow.EIcon.ALERT, "Recipes that contain the currently selected recipe cannot be deleted.", AlertWindow.EAlert.YES).ShowDialog();
                return;
            }


            AlertWindow alertWindow = new AlertWindow("Delete Recipe", "Are you sure you want to delete?", AlertWindow.EAlert.YESNO);
            if (alertWindow.ShowDialog() != true) return;

            string selectedRecipePath = "DB/Recipes" + getAllPath(SelectedNode);
            DirectoryInfo di = new DirectoryInfo(selectedRecipePath);
            di.Delete(true);

            TreeViewModel = new TreeViewModel();
            getAllDirectoriesFromPath("DB/Recipes/");
            SelectedNode = TreeViewModel.Tree.Root.Children[0].Children[0];


            alertWindow = new AlertWindow("Delete Recipe", "Deletion has been completed.", AlertWindow.EAlert.YES);
            alertWindow.ShowDialog();
        }

        private void selectRecipe()
        {
            if (SelectedNode == null) return;
            if (SelectedNode.Data.Name == "Recipes") return;

            AlertWindow alertWindow = new AlertWindow("Select Recipe", "Are you sure you want to apply this recipe?\nIt will be applied immediately upon confirmation.", AlertWindow.EAlert.YESNO);
            if (alertWindow.ShowDialog() != true) return;

            string selectedRecipePath = "DB/Recipes" + getAllPath(SelectedNode);
            string selectedRecipeName = SelectedNode.Data.Name;

            RecipeService.Instance.ChangeRecipe(selectedRecipePath, selectedRecipeName);

            SelectedNode = TreeViewModel.Tree.Root.FindNodeByName(SelectedRecipeName);
            alertWindow = new AlertWindow("Select Recipe", "Recipe application completed.", AlertWindow.EAlert.YES);
            alertWindow.ShowDialog();
        }

        private void renameRecipe()
        {
            if (SelectedNode == null) return;
            if (SelectedNode.Data.Name == "Root" || SelectedNode.Data.Name == "Recipes") return;

            InputTextWindow inputTextWindow = new InputTextWindow("Input the new recipe name", "Name", SelectedNode.Data.Name);
            if (inputTextWindow.ShowDialog() != true) return;

            string newRecipeName = inputTextWindow.xTextBox.Text;

            if (string.IsNullOrEmpty(newRecipeName)) throw new WrongDirectoryException();

            if (this.recipeNames.Contains(newRecipeName))
            {
                new AlertWindow("Exist Name", AlertWindow.EIcon.ALERT, "It is an already existing recipe name.", AlertWindow.EAlert.YES).ShowDialog();
                return;
            }

            string selectedRecipePath = "DB/Recipes" + getAllPath(SelectedNode);
            string newRecipePath = "DB/" + getBasePath(SelectedNode) + "/" + newRecipeName;


            System.IO.Directory.Move(selectedRecipePath, newRecipePath);

            TreeViewModel = new TreeViewModel();
            getAllDirectoriesFromPath("DB/Recipes/");
            SelectedNode = TreeViewModel.Tree.Root.Children[0].Children[0];
            RecipeService.Instance.ChangeRecipe(newRecipePath, newRecipeName);


            AlertWindow alertWindow = new AlertWindow("Rename Recipe", "The recipe name has been successfully changed.", AlertWindow.EAlert.YES);
            alertWindow.ShowDialog();
        }

        private string getAllPath(TreeNodeViewModel? tn)
        {
            string path = "";

            if (tn.Data.Name == "Recipes") return path + "/";

            path = getAllPath(tn.Parent) + tn.Data.Name + "/";

            return path;
        }

        private string getParentPath(TreeNodeViewModel? tn)
        {
            if (tn == null) return "/";
            if (tn.Data.Name == "Recipes") return "/";

            string parentPath = getParentPath(tn.Parent);

            if (parentPath == "/")
            {
                return parentPath + tn.Data.Name;
            }
            else
            {
                return parentPath + "/" + tn.Data.Name;
            }
        }

        private string getBasePath(TreeNodeViewModel? tn)
        {
            if (tn == null) return "/";
            if (tn.Data.Name == "Recipes") return "/";

            string parentPath = getBasePath(tn.Parent);

            if (parentPath == "/") return parentPath + tn.Parent.Data.Name;
            else return parentPath + "/" + tn.Parent.Data.Name;
        }

        private void getAllDirectoriesFromPath(string rootPath)
        {
            DirectoryInfo rootDirectory = new DirectoryInfo(rootPath);
            if (!rootDirectory.Exists) return;

            this.recipeNames.Clear();
            File.WriteAllText(GlobalSetting.Instance.DeviceInfo.VisionRecipePath, string.Empty);
            TreeViewModel.Tree.Root.AddChild(new NodeInfoViewModel() { Name = rootDirectory.Name });
            getAllDirectoriesFromDirectory(TreeViewModel.Tree.Root.Children.Last(), rootDirectory.FullName);
        }

        private void getAllDirectoriesFromDirectory(TreeNodeViewModel? node, string directory)
        {
            var directoryList = Directory.GetDirectories(directory);

            foreach (string dir in directoryList)
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(dir);
                    if (di.Attributes.HasFlag(FileAttributes.System))
                        continue;

                    string recipeName = Path.GetFileName(dir);
                    this.recipeNames.Add(recipeName);

                    node?.AddChild(new NodeInfoViewModel() { Name = recipeName });
                    addLineToFile(GlobalSetting.Instance.DeviceInfo.VisionRecipePath, recipeName);

                    getAllDirectoriesFromDirectory(node?.Children.Last(), dir);
                }
                catch (System.UnauthorizedAccessException)
                {
                    continue;
                }
            }
        }

        static void addLineToFile(string filePath, string content)
        {
            using StreamWriter writer = new StreamWriter(filePath, true);
            writer.WriteLine(content);
        }

        private void toggleMappingInspection()
        {
            IsMappingUsed = !IsMappingUsed;
        }

        private void togglePrsInspection()
        {
            IsPrsUsed = !IsPrsUsed;
        }

        private void toggleBarCodeInspection()
        {
            IsBarcodeUsed = !IsBarcodeUsed;
        }

        private static ObservableCollection<string> getSubdirectories(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new ObservableCollection<string>();
            }

            string[] subdirectoryEntries = Directory.GetDirectories(directoryPath);
            ObservableCollection<string> subdirectoryNames = new ObservableCollection<string>();

            foreach (string subdirectory in subdirectoryEntries)
            {
                subdirectoryNames.Add(Path.GetFileName(subdirectory));
            }

            return subdirectoryNames;
        }

        private void checkTeachingFiles()
        {
            Device device = DeviceRecipeRepository.Instance.GetRecipe();

            InspectionTeaching? mappingRecipe = device.MapPackageType switch
            {
                EInspection.Mark => GridMoldRepository.Instance.GetRecipe(),
                EInspection.Qfn => GridQfnRepository.Instance.GetRecipe(),
                EInspection.Bga => GridBgaRepository.Instance.GetRecipe(),
                EInspection.Lga => GridLgaRepository.Instance.GetRecipe(),
                _ => null
            };

            InspectionTeaching? prsRecipe = device.PrsPackageType switch
            {
                EInspection.Mark => MoldRepository.Instance.GetRecipe(),
                EInspection.Bga => BgaRepository.Instance.GetRecipe(),
                EInspection.Qfn => QfnRepository.Instance.GetRecipe(),
                EInspection.Lga => LgaRepository.Instance.GetRecipe(),
                _ => null
            };

            HasMappingRecipe = mappingRecipe?.IsTaught ?? false;
            HasPrsRecipe = prsRecipe?.IsTaught ?? false;


            InspectionTeaching StripTeaching = StripRepository.Instance.GetRecipe();
            HasBarCodeRecipe = StripTeaching.IsTaught;
        }

    }
}