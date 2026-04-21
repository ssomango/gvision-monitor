using Dapper;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Entities.Lot;
using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.Frames.Windows;
using System.Threading.Tasks;
using System.Windows;

namespace GVisionWpf.Services
{
    public class LotService
    {
        private static readonly Lazy<LotService> lazy = new Lazy<LotService>(() => new LotService());
        public static LotService Instance => lazy.Value;

        private readonly LotRepository lotRepository;
        private readonly InspectionResultRepository inspectionResultRepository = InspectionResultRepository.Instance;

        private LotService()
        {
            this.lotRepository = LotRepository.Instance;
        }

        public async Task<(IEnumerable<LotAntifragile>, int)> GetPaginatedLot(int currentPage = 1, int pageSize = 10)
        {
            (IEnumerable<LotAntifragile> lot, int totalCount) = await this.lotRepository.FindAllBy(currentPage, pageSize);
            return (lot, totalCount);
        }

        public async Task<LotAntifragile> CreateLot(string package, string? lotNumber, DateTime startTime, DateTime endTime)
        {
            return await this.lotRepository.Save(new LotAntifragile
            {
                Package = package,
                LotNumber = lotNumber,
                StartTime = startTime,
                EndTime = endTime
            });
        }

        public async Task<LotAntifragile> FindById(int id)
        {
            return await this.lotRepository.FindById(id);
        }

        public async Task<IEnumerable<LotAntifragile>> FindAll()
        {
            return await this.lotRepository.FindAll();
        }

        public async Task<List<LotAntifragile>> FindAllByLotNumber(string lotNumber)
        {
            DynamicParameters whereLot = new DynamicParameters();
            whereLot.Add("LotNumber", lotNumber);

            IEnumerable<LotAntifragile> enumerableLot = await this.lotRepository.FindAllBy(whereLot);
            List<LotAntifragile> lots = enumerableLot.ToList();

            return lots;
        }

        public async Task UpdateLotEnd()
        {
            LotAntifragile lot = await this.lotRepository.FindById(GlobalSetting.Instance.DeviceInfo.LotId);
            lot.EndTime = DateTime.Now;
            await this.lotRepository.Update(lot);

            string statistics = await MakeStatistics(GlobalSetting.Instance.DeviceInfo.LotId);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                new AlertWindow("Lot Statistics", statistics, AlertWindow.EAlert.TEXT, TimeSpan.FromSeconds(60)).ShowDialog();
            });
        }

        public async Task<InspectionResultAntifragile> CreateInspectionResult(EInspection inspectionType, InspectionResult inspectionResult, int column, int row, int lotId)
        {
            InspectionResultAntifragile inspectionResultEntity = new InspectionResultAntifragile
            {
                LotId = lotId,
                RecipeName = GlobalSetting.Instance.DeviceInfo.RecipeName,
                Duration = inspectionResult.Duration,
                StartTime = inspectionResult.StartTime,
                EndTime = inspectionResult.StartTime.AddSeconds(inspectionResult.Duration), // TODO: 이게 뭐지?
                Item = inspectionResult.EvaluateResults() ? "PASS" : inspectionResult.ErrorType() + " (FAIL)",
                XPos = column,
                YPos = row,
                XOffset = inspectionResult.PackageOffset.Value.X,
                YOffset = inspectionResult.PackageOffset.Value.Y,
                TOffset = inspectionResult.PackageOffset.Value.T,
                PackageWidth = inspectionResult.PackageSize.Value.Width,
                PackageHeight = inspectionResult.PackageSize.Value.Height,
                HasDevice = inspectionResult.HasDevice.Value,
                InspectionType = inspectionType,
            };

            return await this.inspectionResultRepository.Save(inspectionResultEntity);
        }

        public async Task<(string, int)> MakeStatisticsByInspectionType(EInspection inspectionType, int lotId)
        {
            IEnumerable<ErrorCount> errorCounts = this.inspectionResultRepository.GetErrorCounts(lotId, inspectionType);

            string result = "";

            string DeviceType = $"[{inspectionType}]\n";
            string header = "";
            string errorPart = "";

            int totalCount = 0;
            int passCount = 0;
            int failCount = 0;

            foreach (ErrorCount errorCount in errorCounts)
            {
                if (errorCount.ErrorType == "PASS")
                {
                    passCount = errorCount.Count;
                    continue;
                }

                if (errorCount.ErrorType == "Total")
                {
                    totalCount = errorCount.Count;
                    continue;
                }

                failCount += errorCount.Count;
                errorPart += $"- {errorCount.ErrorType} : {errorCount.Count}\n";
            }


            if (totalCount == 0)
            {
                header += $"YEILD: 100%\n";
            }
            else
            {
                float yield = (((float)passCount / (float)totalCount) * 100);
                yield = (float)Math.Round(yield, 3);

                header += $"YEILD: {yield}%\n";
            }


            header += $"TOTAL : {totalCount}\n";
            header += $"PASS : {passCount}\n";
            header += $"FAIL : {failCount}\n";


            result += DeviceType;
            result += header;
            result += errorPart;
            result += "\n";

            return (result, totalCount);
        }

        public async Task<string> MakeStatistics(int lotId)
        {
            LotAntifragile lot = await this.FindById(lotId);

            string result = "";
            result += $"[LOT LOG INFORMATION : {lot.Package}]\n";
            result += $"StartTime: {lot.StartTime}\n";
            result += $"EndTime: {lot.EndTime}\n\n";


            foreach (EInspection inspection in Enum.GetValues(typeof(EInspection)))
            {
                var (statString, totalCount) = await this.MakeStatisticsByInspectionType(inspection, lotId);

                if (totalCount == 0)
                {
                    continue;
                }

                result += statString;
            }

            return result;
        }

    }
}
