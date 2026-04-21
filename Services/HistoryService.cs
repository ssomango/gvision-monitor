using Dapper;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.History;
using GVisionWpf.Models.Entities.Lot;
using GVisionWpf.Repositories;
using System.Threading.Tasks;

namespace GVisionWpf.Services
{
    public class HistoryService
    {
        private static readonly Lazy<HistoryService> lazy = new Lazy<HistoryService>(() => new HistoryService());
        public static HistoryService Instance => lazy.Value;

        private readonly HistoryRepository historyRepository = HistoryRepository.Instance;

        public HistoryService() { }

        public async Task<(IEnumerable<HistoryAntifragile>, int)> GetPaginatedHistories(ECamera? camera = null,
                                                                                        EInspection? inspection = null, ELog? logType = null,
                                                                                        string? lotNumber = null, string? package = null,
                                                                                        int currentPage = 1, int pageSize = 10)
        {
            var where = new DynamicParameters();

            if (camera.HasValue && camera != ECamera.NotSelected)
            {
                where.Add("Camera", camera);
            }

            if (inspection.HasValue && inspection != EInspection.NotSelected)
            {
                where.Add("Inspection", inspection);
            }

            if (logType.HasValue && logType != ELog.ShowAllLogs)
            {
                where.Add("LogType", logType);
            }

            if (!string.IsNullOrEmpty(lotNumber) && lotNumber != "All Lot Data")
            {
                DynamicParameters whereLot = new DynamicParameters();
                whereLot.Add("LotNumber", lotNumber);

                IEnumerable<LotAntifragile> enumerableLots = await LotRepository.Instance.FindAllBy(whereLot);
                List<LotAntifragile> lots = enumerableLots.ToList();

                if (lots.Count != 0)
                {
                    where.Add("LotId", lots[0].Id);
                }
            }

            if (!string.IsNullOrEmpty(package) && package != "All Packages")
            {
                where.Add("Package", package);
            }

            var (histories, totalCount) = await this.historyRepository.FindAllBy(where, currentPage, pageSize);
            return (histories, totalCount);
        }

        public async Task<(IEnumerable<HistoryAntifragile>, int)> GetPaginatedHistories(ECamera? camera = null,
                                                                                        EInspection? inspection = null, ELog? logType = null,
                                                                                        string? lotNumber = null, string? package = null,
                                                                                        int currentPage = 1, int pageSize = 10,
                                                                                        DateTime? startTime = null, DateTime? endTime = null)
        {
            if (startTime == null || endTime == null)
            {
                return await GetPaginatedHistories(camera, inspection, logType, lotNumber, package, currentPage, pageSize);
            }

            var where = new DynamicParameters();

            if (camera.HasValue && camera != ECamera.NotSelected)
            {
                where.Add("Camera", camera);
            }

            if (inspection.HasValue && inspection != EInspection.NotSelected)
            {
                where.Add("Inspection", inspection);
            }

            if (logType.HasValue && logType != ELog.ShowAllLogs)
            {
                where.Add("LogType", logType);
            }

            if (!string.IsNullOrEmpty(lotNumber) && lotNumber != "All Lot Data")
            {
                DynamicParameters whereLot = new DynamicParameters();
                whereLot.Add("LotNumber", lotNumber);

                IEnumerable<LotAntifragile> lot = await LotRepository.Instance.FindAllBy(whereLot);
                List<LotAntifragile> lots = lot.ToList();

                if (lots.Count != 0)
                {
                    where.Add("LotId", lots[0].Id);
                }
            }

            if (!string.IsNullOrEmpty(package) && package != "All Packages")
            {
                where.Add("Package", package);
            }

            endTime = endTime.Value.AddDays(1);
            var (histories, totalCount) = await this.historyRepository.FindAllBy(where, currentPage, pageSize, startTime.Value, endTime.Value);
            return (histories, totalCount);
        }

        public async Task CreateHistory(int lotId, string recipeName, string description, ELog logType, ECamera cameraType, EInspection inspection, string? imagePath)
        {
            await this.historyRepository.Save(new HistoryAntifragile
            {
                LotId = lotId,
                Package = recipeName,
                Camera = cameraType,
                Inspection = inspection,
                Description = description,
                LogType = logType,
                ImagePath = imagePath
            });
        }

        public async Task CreateHistory(String description, ELog logType)
        {
            await this.historyRepository.Save(new HistoryAntifragile
            {
                Camera = null,
                Description = description,
                LogType = logType
            });
        }

        public async Task CreateHistory(string title, ICollection<AnyDiff.Difference> differences)
        {
            string log = $"[{title} Changed]\n";
            log += "=============================\n";
            log += $"Recipe: {GlobalSetting.Instance.DeviceInfo.RecipeName}\n";
            log += "=============================\n";

            foreach (var difference in differences)
            {
                object? oldValue = difference.LeftValue;
                object? newValue = difference.RightValue;

                if (difference.Property.Contains("Property"))
                {
                    continue;
                }

                if (difference.Property.Contains("Handle"))
                {
                    continue;
                }

                if (difference.PropertyType == typeof(double))
                {
                    oldValue = Math.Round((double)oldValue, 3);
                    newValue = Math.Round((double)newValue, 3);
                }

                if (difference.PropertyType == typeof(float))
                {
                    oldValue = (float)Math.Round((float)oldValue, 3);
                    newValue = (float)Math.Round((float)newValue, 3);
                }

                log += $"{difference.Path} : {oldValue} => {newValue}\n";
            }

            await CreateHistory(log, ELog.RecipeLogs);
        }

        public async Task<HistoryAntifragile> FindById(int id)
        {
            return await this.historyRepository.FindById(id);
        }

        public async Task<string> GetHistory(int id)
        {
            HistoryAntifragile history = await this.historyRepository.FindById(id);

            return $"Id: {history.Id}\n" +
                   $"Time: {history.Time:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Package: {history.Package ?? "N/A"}\n" +
                   $"LotId: {history.LotId?.ToString() ?? "N/A"}\n" +
                   $"Camera: {history.Camera?.ToString() ?? "N/A"}\n" +
                   $"Inspection: {history.Inspection?.ToString() ?? "N/A"}\n" +
                   $"LogType: {history.LogType?.ToString() ?? "N/A"}\n" +
                   $"Description: {history.Description ?? "N/A"}\n" +
                   $"ImagePath: {history.ImagePath ?? "N/A"}\n";
        }
    }
}