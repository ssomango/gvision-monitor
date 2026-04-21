using GVisionWpf.Events.Message.Packet;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Entities.Lot;
using GVisionWpf.Repositories;
using GVisionWpf.Services;
using GVisionWpf.UIs.ViewModels;
using log4net;
using System.IO;
using System.Threading.Tasks;

namespace GVisionWpf.PresentationLayer.Controllers
{
    public class LotController : BaseController
    {
        private static readonly Lazy<LotController> lazy = new Lazy<LotController>(() => new LotController());
        public static LotController Instance => lazy.Value;

        private readonly LotService lotService;

        private EmapRepository emapRepository = EmapRepository.Instance;

        private static readonly ILog log = LogManager.GetLogger("LOT");

        private LotController()
        {
            this.lotService = LotService.Instance;
        }

        public async Task StartNewLot(CommonBody body)
        {
            log.Info("[Request] LOT Start");

            string newLot = "";

            handleStartNewLot();

            if (File.Exists(GlobalSetting.Instance.DeviceInfo.CurLotPath))
            {
                using StreamReader reader = new StreamReader(GlobalSetting.Instance.DeviceInfo.CurLotPath);
                newLot = await reader.ReadLineAsync() ?? throw new VisionIfException();
            }
            else
            {
                throw new VisionIfException();
            }

            List<LotAntifragile> lots = await this.lotService.FindAllByLotNumber(newLot);

            if (lots.Count != 0)
            {
                throw new DuplicatedLotNumberException();
            }

            if (GlobalSetting.Instance.DeviceInfo.LotNumber != newLot)
            {
                await this.lotService.CreateLot(GlobalSetting.Instance.DeviceInfo.RecipeName, newLot, DateTime.Now, DateTime.Now);

                lots = await this.lotService.FindAllByLotNumber(newLot);

                if (lots.Count == 0)
                {
                    throw new GVisionException();
                }

                GlobalSetting.Instance.DeviceInfo.LotNumber = newLot;
                GlobalSetting.Instance.DeviceInfo.LotId = lots[0].Id;
                GlobalSetting.Instance.Persist();
                GlobalSetting.Instance.ApplySetting();
            }

            GVisionMessenger.Instance.UI.SendSystemInfoMessage("New Lot Started!");
        }


        public async Task EndLot(CommonBody body)
        {
            log.Info("[Request] LOT End");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("The Lot Ended!");
            await this.lotService.UpdateLotEnd();
            await handleLotEnd();
        }

        private void handleStartNewLot()
        {
            if (GlobalSetting.Instance.SystemType == ESystemType.HanaMicron)
            {
                GVisionMessenger.Instance.Packet.SendPacketHandlingMessage(PacketMessage.EPacketMessageAction.ShouldTeachNewMarks);
            }
        }

        private async Task handleLotEnd()
        {
            switch (GlobalSetting.Instance.SystemType)
            {
                case ESystemType.HanaMicron:
                    await this.emapRepository.DeleteAll();
                    break;

                default:
                    return;
            }
        }
    }
}