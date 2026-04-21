using GVisionWpf.DSMMI.Dto;
using GVisionWpf.DSMMI.UI;
using GVisionWpf.Models.Dtos;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.Visions;
using GVisionWpf.UIs.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.DSMMI.Inspection
{
    internal class Prs : IBytesConvertible
    {
        private static readonly Lazy<Prs> lazy = new Lazy<Prs>(() => new Prs());
        public static Prs Instance => lazy.Value;

        private ECameraTriggerMode cameraTriggerMode = ECameraTriggerMode.HardwareTrigger;

        private const uint SW_PACKET = 0u;
        private const uint HW_PACKET = 1u;
        private const uint TEST_PACKET = 999;


        private void sendMockPrsPacket(ECameraTriggerMode triggerType, List<PickPosition> positions)
        {
            switch (triggerType)
            {
                case ECameraTriggerMode.HardwareTrigger:
                case ECameraTriggerMode.IOTrigger:
                    sendMockPrsPacketForHwTrigger(positions);
                    break;
                case ECameraTriggerMode.SoftwareTrigger:
                    sendMockPrsPacketForSwTrigger(positions);
                    break;
            }
        }

        private void sendMockPrsPacketForSwTrigger(List<PickPosition> positions)
        {
            List<PrsRequest> requests = Enumerable.Range(0, 8).Select(_ => new PrsRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xffffffff,
                    DataLength = 0x3001,
                    CommonHeader = 0x01000100,
                    CameraId = 0x01,
                    InspectionType = 0x0a
                },
                TriggerType = TEST_PACKET,
                CaptureDone = 0x00,
                PrsBodies = new List<EachPrsBody>()
            }).ToList();

            for (int requestNum = 0; requestNum < requests.Count; requestNum++)
            {
                foreach (PickPosition pos in positions)
                {
                    VinsFantasyViewModel.Instance.Print($"[AUTO RUN][REQ] {pos.X}, {pos.Y}, {pos.Z}");
                    requests[requestNum].PrsBodies.Add(
                        new EachPrsBody
                        {
                            StripBarcode = 0,
                            Sequence = 0,
                            GridTableNumber = 0x00,
                            X1Orx2 = 0x01,
                            ZAxisNum = requestNum + 1 == pos.Z ? pos.Z : 0xff, // 자재 잡고 있는 피커 번호
                            HasDevice = 0x01,
                            XPickPosition = pos.X,
                            YPickPosition = pos.Y
                        });

                };
            }

            requests.ForEach(request => VinsCommunicator.Instance.Send(request));
        }

        private void sendMockPrsPacketForHwTrigger(List<PickPosition> positions)
        {
            VinsFantasyViewModel.Instance.Print("[AUTO RUN][REQ] PRS 8");

            PrsRequest request = new PrsRequest
            {
                CommonBody = new CommonBody
                {
                    Prefix = 0xffffffff,
                    DataLength = 0x3001,
                    CommonHeader = 0x01000100,
                    CameraId = 0x01,
                    InspectionType = 0x0a
                },
                TriggerType = TEST_PACKET,
                CaptureDone = 0x00,
                PrsBodies = new List<EachPrsBody>()
            };

            foreach (PickPosition pos in positions)
            {
                VinsFantasyViewModel.Instance.Print($"[AUTO RUN][REQ] {pos.X}, {pos.Y}, {pos.Z}");
                request.PrsBodies.Add(
                    new EachPrsBody
                    {
                        StripBarcode = 0,
                        Sequence = 0,
                        GridTableNumber = 0x00,
                        X1Orx2 = 0x01,
                        ZAxisNum = pos.Z, // 자재 잡고 있는 피커 번호
                        HasDevice = 0x01,
                        XPickPosition = pos.X,
                        YPickPosition = pos.Y
                    }
                );
            }

            VinsCommunicator.Instance.Send(request);
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            List<PickPosition> poss = new List<PickPosition>();

            uint z = 8;
            int maxDeviceSize = PrsDeviceViewViewModel.Instance.VisionTableLayout.Row * PrsDeviceViewViewModel.Instance.VisionTableLayout.Col;
            for (uint i = 0; i < maxDeviceSize + 8; i++, z--)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (z == 0)
                {
                    z = 8;
                    poss.Reverse();

                    sendMockPrsPacket(cameraTriggerMode, poss);

                    poss.Clear();
                    await Task.Delay(VinsFantasyViewModel.Instance.PrsDelay);
                }

                uint x = i % (uint)PrsDeviceViewViewModel.Instance.VisionTableLayout.Col;
                uint y = i / (uint)PrsDeviceViewViewModel.Instance.VisionTableLayout.Col;

                poss.Add(new PickPosition(x, y, z));
            }
        }
    }
}
