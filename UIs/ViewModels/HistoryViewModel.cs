using CommunityToolkit.Mvvm.Input;
using GVisionWpf.Api;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Models.Entities.History;
using GVisionWpf.Models.Entities.Lot;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.Frames.Windows;
using GVisionWpf.UIs.Frames.Windows.Teaching;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.UIs.ViewModels.Command;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        private static readonly Lazy<HistoryViewModel> lazy = new Lazy<HistoryViewModel>(() => new HistoryViewModel());
        public static HistoryViewModel Instance => lazy.Value;

        private ObservableCollection<string> databaseNames;
        private string selectedDatabaseName;
        private DateTime selectedBeforeDateTime = new DateTime(2024, 7, 1);
        private DateTime selectedAfterDateTime = DateTime.Now;

        private ObservableCollection<string> recipeNames;
        private ObservableCollection<string> lotData;
        private readonly List<ECamera> cameras;
        private readonly List<EInspection> inspections;
        private readonly List<ELog> logType;
        private string? selectedRecipeName;
        private string selectedLotData;
        private ECamera selectedCamera;
        private EInspection selectedInspection;
        private ELog selectedLogType;

        private ObservableCollection<History> histories;
        private int selectedIndex;

        private ObservableCollection<History> selectedItems = new ObservableCollection<History>();
        private DelegateCommand<object> selectionChangeCommand;

        private String descriptionTextBoxText = "";

        public HSmartWindowControlWPF SmartWindow { get; set; }

        private readonly string dbPath = "DB/Schema/";
        private readonly string filePath = "DB/History/";
        private readonly HistoryService historyService = HistoryService.Instance;
        private bool isLoading;
        private int currentPage = 1;
        private const int PAGE_SIZE = 20;

        #region Property

        public ICommand SaveCommand { get; private set; }
        public ICommand FilterApplyCommand { get; private set; }
        public ICommand LoadMoreDataCommand { get; private set; }
        public ICommand OpenButtonClickCommand { get; private set; }
        public DelegateCommand<object> SelectionChangeCommand { get; private set; }

        public ObservableCollection<string> DatabaseNames
        {
            get
            {
                return this.databaseNames;
            }
            set
            {
                this.databaseNames = value;
                OnPropertyChanged();
            }
        }
        public string SelectedDatabaseName
        {
            get
            {
                return this.selectedDatabaseName;
            }
            set
            {
                this.selectedDatabaseName = value;
                UpdateConnectionString("ConnectionString", $"Data Source={this.dbPath}{value}.db");
                OnPropertyChanged();
            }
        }
        public DateTime SelectedBeforeDateTime
        {
            get => this.selectedBeforeDateTime;
            set
            {
                this.selectedBeforeDateTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime SelectedAfterDateTime
        {
            get => this.selectedAfterDateTime;
            set
            {
                this.selectedAfterDateTime = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> RecipeNames
        {
            get => this.recipeNames;
            set
            {
                this.recipeNames = value;
                OnPropertyChanged();
            }
        }

        public string SelectedRecipeName
        {
            get => this.selectedRecipeName;
            set
            {
                this.selectedRecipeName = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> LotData
        {
            get => this.lotData;
            set
            {
                this.lotData = value;
                OnPropertyChanged();
            }
        }

        public string SelectedLotData
        {
            get => this.selectedLotData;
            set
            {
                this.selectedLotData = value;
                OnPropertyChanged();
            }
        }

        public List<ECamera> Cameras { get => this.cameras; }

        public ECamera SelectedCamera
        {
            get => this.selectedCamera;
            set
            {
                this.selectedCamera = value;
                OnPropertyChanged();
            }
        }

        public List<EInspection> Inspections { get => this.inspections; }

        public EInspection SelectedInspection
        {
            get => this.selectedInspection;
            set
            {
                this.selectedInspection = value;
                OnPropertyChanged();
            }
        }

        public List<ELog> LogType { get => this.logType; }

        public ELog SelectedLogType
        {
            get => this.selectedLogType;
            set
            {
                this.selectedLogType = value;
                OnPropertyChanged();
            }
        }

        public string DescriptionTextBoxText
        {
            get => this.descriptionTextBoxText;
            set
            {
                this.descriptionTextBoxText = value;
                OnPropertyChanged();
            }
        }

        public int SelectedIndex
        {
            get => this.selectedIndex;
            set
            {
                this.selectedIndex = value;
                if (this.selectedIndex >= 0 && this.selectedIndex < this.histories.Count)
                {
                    DescriptionTextBoxText = this.histories[this.selectedIndex].Description ?? "";

                    SmartWindow.HalconWindow.ClearWindow();
                    string? imagePath = this.histories[this.selectedIndex].ImagePath;

                    if (imagePath != null)
                    {
                        HObject image;
                        try
                        {
                            HOperatorSet.ReadImage(out image, imagePath);
                        }
                        catch
                        {
                            HOperatorSet.GenEmptyObj(out image);
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SmartWindow.HalconWindow.DispObj(image);
                            SmartWindow.SetFullImagePart();
                        });

                    }
                }
                OnPropertyChanged();
            }
        }

        public ObservableCollection<History> HistoryDataGridItemsSource
        {
            get => this.histories;
            set
            {
                this.histories = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<History> SelectedItems
        {
            get => this.selectedItems;
            set
            {
                this.selectedItems = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public HistoryViewModel()
        {
            this.selectionChangeCommand = new DelegateCommand<object>(executeSelectionChangeCommand);

            SaveCommand = new RelayCommand(saveHistory);
            FilterApplyCommand = new RelayCommand(applyFilter);
            LoadMoreDataCommand = new RelayCommand(async () => await loadMoreData(), () => !this.isLoading);
            OpenButtonClickCommand = new RelayCommand(openTeachingWindow);
            SelectionChangeCommand = this.selectionChangeCommand;

            this.databaseNames = getDbFiles(this.dbPath);
            SelectedDatabaseName = this.databaseNames[0];

            this.recipeNames = getSubdirectories("DB/Recipes/");
            this.recipeNames.Add("All Packages");

            // 임시완: 임시로 모든 lot nubmer를 가져옵니다. 나중에 수정해주세요
            this.lotData = Task.Run(getAllLotName).Result;

            this.cameras = Enum.GetValues(typeof(ECamera)).Cast<ECamera>().ToList();
            this.inspections = Enum.GetValues(typeof(EInspection)).Cast<EInspection>().ToList();
            this.logType = Enum.GetValues(typeof(ELog)).Cast<ELog>().ToList();

            this.selectedRecipeName = "All Packages";
            this.selectedLotData = "All Lot Data";
            this.selectedCamera = ECamera.NotSelected;
            this.selectedInspection = EInspection.NotSelected;
            this.selectedLogType = ELog.ShowAllLogs;
            this.histories = new ObservableCollection<History>();

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            Task.Run(loadMoreData);
        }

        [RelayCommand]
        private async Task deleteSelectedItems()
        {
            if (SelectedItems.IsNullOrEmpty()) return;

            var selectedItemCount = SelectedItems.Count;

            await Parallel.ForEachAsync(SelectedItems, async (history, cancellationToken) =>
            {
                await HistoryRepository.Instance.Delete(history.Id);
            });

            var result = MessageBox.Show($"Delete {selectedItemCount} histories");

            if (result == MessageBoxResult.OK)
            {
                applyFilter();
            }
        }

        private async Task loadMoreData()
        {
            try
            {
                if (this.isLoading) return;
                this.isLoading = true;

                var (enumerableHistory, totalCount) = await this.historyService.GetPaginatedHistories(
                    this.selectedCamera,
                    this.selectedInspection,
                    this.selectedLogType,
                    this.selectedLotData,
                    this.selectedRecipeName,
                    this.currentPage,
                    PAGE_SIZE,
                    this.selectedBeforeDateTime,
                    this.selectedAfterDateTime
                );

                foreach (HistoryAntifragile antifragile in enumerableHistory)
                {
                    LotAntifragile? lot = null;

                    if (antifragile.LotId != null)
                    {
                        lot = await LotService.Instance.FindById((int)antifragile.LotId);
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        HistoryDataGridItemsSource.Add(new History
                        {
                            Id = antifragile.Id,
                            Time = antifragile.Time,
                            Package = antifragile.Package,
                            LotNumber = lot?.LotNumber,
                            Camera = antifragile.Camera,
                            Inspection = antifragile.Inspection,
                            LogType = antifragile.LogType,
                            Description = antifragile.Description,
                            ImagePath = antifragile.ImagePath
                        });
                    });
                }

                this.currentPage++;
                this.isLoading = false;

                SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                GlobalErrorHandler.HandleException(ex);
            }
        }

        // 임시
        private async Task<ObservableCollection<string>> getAllLotName()
        {
            var enumerableLot = await LotService.Instance.FindAll();
            ObservableCollection<string> result = new ObservableCollection<string>();
            result.Add("All Lot Data");

            foreach (var l in enumerableLot)
                result.Add(l.LotNumber ?? throw new InvalidOperationException());

            return result;
        }

        private void openTeachingWindow()
        {
            EInspection? inspection = this.histories[this.selectedIndex].Inspection;
            string? imagePath = this.histories[this.selectedIndex].ImagePath;

            if (inspection == null || inspection == EInspection.NotSelected || imagePath == null)
            {
                return;
            }

            HImage image = new HImage(imagePath);

            switch (inspection)
            {
                case EInspection.Mapping:
                    new AlertWindow("Unable to open", AlertWindow.EIcon.ALERT, "Unable to open the Mapping Teaching window.\nThis feature is not supported.", AlertWindow.EAlert.YES).ShowDialog();
                    // new GridMoldTeachingWindow { TeachingImage = image, }.ShowDialog();
                    break;
                case EInspection.Qfn:
                    new QfnTeachingWindow { TeachingImage = image }.ShowDialog();
                    break;
                case EInspection.Bga:
                    new BgaTeachingWindow() { TeachingImage = image }.ShowDialog();
                    break;
                case EInspection.Lga:
                    new LgaTeachingWindow { TeachingImage = image }.ShowDialog();
                    break;
            }
        }

        private async void saveHistory()
        {
            string historyString = "";
            string fileName = $"{this.filePath}history_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

            foreach (History history in SelectedItems)
            {
                historyString += await this.historyService.GetHistory(history.Id) + "\n\n";
            }

            await File.WriteAllTextAsync(fileName, historyString);

            MessageBox.Show($"History saved\npath: DB/History/\nfile name: {fileName}");
        }

        private async void applyFilter()
        {
            this.currentPage = 1;
            HistoryDataGridItemsSource.Clear();
            await loadMoreData();
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

        private static ObservableCollection<string> getDbFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new ObservableCollection<string>();
            }

            string[] dbFileEntries = Directory.GetFiles(directoryPath, "*.db");
            ObservableCollection<string> dbFileNames = new ObservableCollection<string>();

            foreach (string dbFile in dbFileEntries)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(dbFile);
                dbFileNames.Add(fileNameWithoutExtension);
            }

            return dbFileNames;
        }

        private void executeSelectionChangeCommand(object parameter)
        {
            IList items = (IList)parameter;
            IEnumerable<History> collection = items.Cast<History>();

            if (collection == null) { return; }

            SelectedItems = new ObservableCollection<History>(collection);
        }

        public static void UpdateConnectionString(string name, string newConnectionString)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            ConnectionStringsSection section = (ConnectionStringsSection)config.GetSection("connectionStrings");

            if (section.ConnectionStrings[name] != null)
            {
                section.ConnectionStrings[name].ConnectionString = newConnectionString;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("ConnectionStrings");
            }
            else
            {
                throw new Exception("Connection string not found.");
            }
        }




    }
}
