using Dapper;
using GVisionWpf.DomainLayer.Extensions;
using GVisionWpf.Models.Entities.Emap;
using GVisionWpf.Repositories;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GVisionWpf.Services
{
    public class EmapService
    {
        private static readonly Lazy<EmapService> lazy = new Lazy<EmapService>(() => new EmapService());
        public static EmapService Instance => lazy.Value;
        private EmapRepository emapRepository;

        public EmapService()
        {
            this.emapRepository = EmapRepository.Instance;
        }

        public async Task<int> SaveEmaps(List<EmapEntity> emapAntifragiles)
        {
            return await this.emapRepository.SaveAll(emapAntifragiles);
        }

        public async Task<int> GetStripNumber()
        {
            return await this.emapRepository.GetStripNumber();
        }


        public async Task<List<EmapEntity>>? IsXOut(int tableNumber, int xPickPosition, int yPickPosition)
        {
            int stripNumber = await GetStripNumber();

            DynamicParameters whereXOut = new DynamicParameters();

            whereXOut.Add("XPickPosition", xPickPosition);
            whereXOut.Add("YPickPosition", yPickPosition);
            whereXOut.Add("StripNumber", stripNumber);
            whereXOut.Add("TableNumber", tableNumber);
            whereXOut.Add("Data", new[] { (int)EEmapDataType.XOUT_1, (int)EEmapDataType.XOUT_2 });

            var result = await this.emapRepository.FindAllByWithInCondition(whereXOut, ["Data"]);

            if (!result.ToList().IsNullOrEmpty())
            {
                Debug.Print("XOUT");
            }

            return result.ToList();
        }

    }
}
