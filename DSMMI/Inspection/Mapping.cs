using GVisionWpf.DSMMI.UI;
using GVisionWpf.Models.Dtos;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.Models.UiModels;
using GVisionWpf.UIs.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.DSMMI.Inspection
{
    public class Mapping : IBytesConvertible
    {
        private static readonly Lazy<Mapping> lazy = new Lazy<Mapping>(() => new Mapping());
        public static Mapping Instance => lazy.Value;

        private ECameraTriggerMode cameraTriggerMode = ECameraTriggerMode.IOTrigger;

        public async void RunEachMapping(uint x, uint y)
        {
            VinsFantasyViewModel.Instance.Print($"[AUTO RUN][REQ] Mapping (x:{x}, y:{y})");

            MapRequest req = new MapRequest()
            {
                CommonBody = new CommonBody()
                {
                    Prefix = 0xffffffff,
                    DataLength = 0x50,
                    CommonHeader = 0x01000100,
                    CameraId = 0x00,
                    InspectionType = 0x01,
                },
                TriggerType = 999,
                CaptureDone = 0x01,
                MapBody = new MapBody()
                {
                    StripBarcode = 0x00,
                    GridTableNum = 0,
                    XPosition = x,
                    YPosition = y
                }
            };

            VinsCommunicator.Instance.Send(req);
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            TableLayout trayLayout = MapDeviceViewViewModel.Instance.VisionTableLayout;
            TableLayout fovLayout = MapDeviceViewViewModel.Instance.FovLayout;

            for (uint y = 0; y < (uint)Math.Ceiling((double)trayLayout.Row / fovLayout.Row); y++)
            {
                for (uint x = 0; x < (uint)Math.Ceiling((double)trayLayout.Col / fovLayout.Col); x++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    Instance.RunEachMapping(x, y);

                    if (cameraTriggerMode == ECameraTriggerMode.IOTrigger)
                        return;
                    
                    await Task.Delay(VinsFantasyViewModel.Instance.MapDelay);
                }
            }
        }
    }
}