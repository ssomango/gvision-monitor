using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Dtos.Response;
using GVisionWpf.Models.Entities.Emap;
using GVisionWpf.Models.Entities.Lot;
using GVisionWpf.Services;
using log4net;
using System.Configuration;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace GVisionWpf.PresentationLayer.Controllers
{
    public class EmapController : BaseController
    {
        private static readonly Lazy<EmapController> lazy = new Lazy<EmapController>(() => new EmapController());
        public static EmapController Instance => lazy.Value;
        private readonly EmapService emapService;
        private readonly LotService lotService;

        private static readonly ILog log = LogManager.GetLogger("EMAP");

        private EmapController()
        {
            this.emapService = EmapService.Instance;
            this.lotService = LotService.Instance;
        }

        public async Task HandleEmapRequest(EmapRequest emapRequest)
        {
            HeartBeatResponse response = new HeartBeatResponse();
            response.CurrentVisionStatus = 0x3F;

            base.Respond(response);

            await saveEmap(emapRequest);
        }

        private async Task saveEmap(EmapRequest emapRequest)
        {
            log.Info("[Request] EMAP");

            List<EmapEntity> emaps = new List<EmapEntity>();

            List<LotAntifragile> lots = await this.lotService.FindAllByLotNumber(GlobalSetting.Instance.DeviceInfo.LotNumber);

            if (lots.Count == 0)
            {
                throw new GVisionException();
            }

            int lotId = lots[0].Id;
            int stripNumber = await this.emapService.GetStripNumber() + 1;
            int tableNumber = (int)emapRequest.GridTableNumber;

            // xout이 없을 경우 대비
            emaps.Add(new EmapEntity
            {
                LotId = lotId,
                XPickPosition = -1,
                YPickPosition = -1,
                StripNumber = stripNumber,
                TableNumber = tableNumber,
                Data = 1
            });

            foreach (EachEmapBody e in emapRequest.EmapBodies)
            {
                emaps.Add(new EmapEntity
                {
                    LotId = lotId,
                    XPickPosition = (int)e.XPickPosition,
                    YPickPosition = (int)e.YPickPosition,
                    StripNumber = stripNumber,
                    TableNumber = tableNumber,
                    Data = (int)e.Data
                });
            }

            int count = await this.emapService.SaveEmaps(emaps);

            log.Info($"{count} EMAP has been stored in the database.");
        }
    }
}
