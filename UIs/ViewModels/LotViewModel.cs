using CommunityToolkit.Mvvm.Input;
using Dapper;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Exceptions;
using GVisionWpf.Models.Entities.Lot;
using GVisionWpf.Models.UiModels;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.UiUpdaters;
using GVisionWpf.UIs.ViewModels.Command;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GVisionWpf.UIs.ViewModels
{
    public partial class LotViewModel : ViewModelBase
    {
        private static readonly Lazy<LotViewModel> lazy = new Lazy<LotViewModel>(() => new LotViewModel());
        public static LotViewModel Instance => lazy.Value;

        private ObservableCollection<string> databaseNames;
        private string selectedDatabaseName = "";

        private ObservableCollection<Lot> lotDataGridItemsSource;
        private int lotDataGridSelectedIndex;

        private ObservableCollection<Lot> selectedItems = new ObservableCollection<Lot>();
        private DelegateCommand<object> selectionChangeCommand;

        private readonly string dbPath = "DB/Schema/";
        private readonly string filePath = "DB/Lot/";
        private readonly LotService lotService = LotService.Instance;
        private bool isLoading;
        private int currentPage = 1;
        private const int PAGE_SIZE = 20;

        #region Property

        public ICommand LoadMoreDataCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public DelegateCommand<object> SelectionChangeCommand { get; private set; }

        public ObservableCollection<string> DatabaseNames
        {
            get { return this.databaseNames; }
            set
            {
                this.databaseNames = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDatabaseName
        {
            get { return this.selectedDatabaseName; }
            set
            {
                this.selectedDatabaseName = value;
                UpdateConnectionString("ConnectionString", $"Data Source={this.dbPath}{value}.db");
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Lot> LotDataGridItemsSource
        {
            get => this.lotDataGridItemsSource;
            set
            {
                this.lotDataGridItemsSource = value;
                OnPropertyChanged();
            }
        }

        public int LotDataGridSelectedIndex
        {
            get => this.lotDataGridSelectedIndex;
            set
            {
                this.lotDataGridSelectedIndex = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Lot> SelectedItems
        {
            get => this.selectedItems;
            set
            {
                this.selectedItems = value;
                OnPropertyChanged();
            }
        }

        public string FilePath
        {
            get => this.filePath;
        }

        #endregion

        public LotViewModel()
        {
            this.selectionChangeCommand = new DelegateCommand<object>(executeSelectionChangeCommand);

            LoadMoreDataCommand = new RelayCommand(async () => await loadMoreData(), () => !this.isLoading);
            SaveCommand = new RelayCommand(SaveStatistic);
            SelectionChangeCommand = this.selectionChangeCommand;

            this.databaseNames = getDbFiles(this.dbPath);
            SelectedDatabaseName = this.databaseNames[0];

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            this.lotDataGridItemsSource = new ObservableCollection<Lot>();
            Task.Run(loadMoreData);
        }

        [RelayCommand]
        private async Task deleteSelectedItems()
        {
            if (SelectedItems.IsNullOrEmpty()) return;

            var selectedItemCount = SelectedItems.Count;

            await Parallel.ForEachAsync(SelectedItems, async (lot, cancellationToken) =>
            {
                await LotRepository.Instance.Delete(lot.Id);
            });

            var result = MessageBox.Show($"Delete {selectedItemCount} lots");

            if (result == MessageBoxResult.OK)
            {
                this.currentPage = 1;
                this.LotDataGridItemsSource.Clear();
                await loadMoreData();
            }
        }

        private async Task loadMoreData()
        {
            try
            {
                if (this.isLoading) return;
                this.isLoading = true;

                var (enumerableLot, totalCount) = await this.lotService.GetPaginatedLot(
                    this.currentPage,
                    PAGE_SIZE
                );

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var lotAntifragile in enumerableLot)
                    {
                        DateTime startTime = lotAntifragile.StartTime.GetValueOrDefault();
                        DateTime endTime = lotAntifragile.EndTime.GetValueOrDefault();

                        Lot lot = new Lot
                        {
                            Id = lotAntifragile.Id,
                            Package = lotAntifragile.Package ?? string.Empty,
                            LotNumber = lotAntifragile.LotNumber ?? string.Empty,
                            StartTime = startTime,
                            EndTime = endTime,
                            Ellapsed = endTime - startTime
                        };
                        LotDataGridItemsSource.Add(lot);
                    }
                });

                this.currentPage++;
                this.isLoading = false;
            }
            catch (Exception ex)
            {
                GlobalErrorHandler.HandleException(ex);
            }
        }

        public async void SaveStatistic()
        {
            try
            {
                foreach (Lot lot in this.selectedItems)
                {
                    DynamicParameters whereLot = new DynamicParameters();
                    whereLot.Add("LotNumber", lot.LotNumber);

                    IEnumerable<LotAntifragile> enumerableLots = await LotRepository.Instance.FindAllBy(whereLot);
                    List<LotAntifragile> lots = enumerableLots.ToList();

                    if (lots.Count == 0)
                    {
                        throw new GVisionException();
                    }

                    string statistics = await LotService.Instance.MakeStatistics(lots[0].Id);
                    string fileName = $"{this.filePath}lot_statistics_{DateTime.Now:yyyy-MM-dd}_{lots[0].LotNumber}.txt";

                    await File.WriteAllTextAsync(fileName, statistics);
                }

                MessageBox.Show($"Statistics saved\npath: DB/Lot/");
            }
            catch (Exception ex)
            {
                GlobalErrorHandler.HandleException(ex);
            }
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
            IEnumerable<Lot> collection = items.Cast<Lot>();

            if (collection == null) { return; }

            SelectedItems = new ObservableCollection<Lot>(collection);
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
