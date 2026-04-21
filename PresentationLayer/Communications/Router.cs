using GVisionWpf.Controllers;
using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Models.Dtos.Request;
using GVisionWpf.PresentationLayer.Controllers;
using log4net;
using System.IO;
using System.Threading.Tasks;

namespace GVisionWpf.PresentationLayer.Communications
{
    public class Router
    {
        private static readonly Lazy<Router> lazy = new Lazy<Router>(() => new Router());
        public static Router Instance => lazy.Value;

        private readonly PrsController prsController;
        private readonly MapController mapController;

        private readonly CalibrationController calibrationController;
        private readonly StripController stripController;
        private readonly LotController lotController;
        private readonly RecipeController recipeController;
        private readonly EmapController emapController;

        /* Common Header */
        private const int CMD_REQ = 0x03;
        private const int CMM_HDR = 0x10000;
        private const int LOT_INF = 0x00010004;
        private const int LOT_END_INF = 0x00010007;
        private const int INSPECTION_REQUEST = 0x00010001;
        private const int RECIPE_CHANGE = 0x00010005;
        private const int EMAP = 0x00010009;


        private static readonly ILog log = LogManager.GetLogger("Communication");


        private Router()
        {
            this.mapController = MapController.Instance;
            this.prsController = PrsController.Instance;

            this.calibrationController = CalibrationController.Instance;
            this.stripController = StripController.Instance;
            this.lotController = LotController.Instance;
            this.recipeController = RecipeController.Instance;
            this.emapController = EmapController.Instance;
        }

        public static CommonBody ParseCommonBody(BinaryReader reader)
        {
            return new CommonBody
            {
                Prefix = reader.ReadUInt32(),
                DataLength = reader.ReadUInt32(),
                CommonHeader = reader.ReadUInt32(),
                CameraId = reader.ReadUInt32(),
                InspectionType = reader.ReadUInt32()
            };
        }

        // 핸들러 요청의 시작점
        public async Task RouteToController(byte[] receivedPacket)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(receivedPacket));

            CommonBody commonBody = ParseCommonBody(reader);

            const uint validPrf = 0xffffffff;
            if (commonBody.Prefix != validPrf)
            {
                log.Warn($"The Prefix is invalid. CommonBody: {commonBody.Prefix}");
                return;
            }

            //if (commonBody.DataLength != receivedPacket.Length)
            //{
            //    log.Warn($"The DataLength of the received packet is incorrect. CommonBody: {commonBody}");
            //}

            if (commonBody.CommonHeader == CMM_HDR && commonBody.CameraId == CMD_REQ)
             {
                Heart.Instance.StatusBeat();
                return;
            }

            if (commonBody.CommonHeader == LOT_INF)
            {
                await this.lotController.StartNewLot(commonBody);
                return;
            }

            if (commonBody.CommonHeader == LOT_END_INF)
            {
                await this.lotController.EndLot(commonBody);
                return;
            }

            if (commonBody.CommonHeader == RECIPE_CHANGE)
            {
                await this.recipeController.ChangeRecipe(commonBody);
                return;
            }
            
            if (commonBody.CommonHeader == EMAP)
            {
                EmapRequest emapRequest = new EmapRequest
                {
                    CommonBody = commonBody,
                    TriggerType = reader.ReadUInt32(),
                    CaptureDone = reader.ReadUInt32(),
                    Sequence = reader.ReadUInt32(),
                    GridTableNumber = reader.ReadUInt32()
                };

                emapRequest.EmapBodies = GlobalSetting.Instance.SystemType switch
                {
                    ESystemType.HanaMicron => ParseEmapBodyAsByte(reader, emapRequest),
                    _ => ParseEmapBodyAsUInt32(reader, emapRequest)
                };

                await this.emapController.HandleEmapRequest(emapRequest);

                return;
            }
           
            if (commonBody.InspectionType == (int)ERequestInspectionType.MAP_INSPECTION)
            {
                MapRequest mapRequest = new MapRequest
                {
                    CommonBody = commonBody,
                    TriggerType = reader.ReadUInt32(),
                    CaptureDone = reader.ReadUInt32()
                };

                mapRequest.MapBody = ParseMapRequestPart(reader, mapRequest);

                this.mapController.MapInspection(mapRequest);

                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.X1_ZIG 
                || commonBody.InspectionType == (int)ERequestInspectionType.X2_ZIG)
            {
                if (Heart.Instance.CurrentVisionMode == EVisionMode.AutoRun) return;
                Heart.Instance.CurrentVisionMode = EVisionMode.Calibration;

                SettingCalibrationRequest calibrationRequest = new SettingCalibrationRequest
                {
                    CommonBody = commonBody,
                    Idk = reader.ReadUInt32(),
                    CaptureDone = reader.ReadUInt32(),
                    X1orX2 = reader.ReadUInt32(),
                };

              

                await this.calibrationController.CalculateSettingJigOffset(calibrationRequest);

                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.BOTTOM_INSPECTION)
            {
                
                PrsRequest prsRequest = new PrsRequest
                {
                    CommonBody = commonBody
                };
                prsRequest.PrsBodies = ParsePrsRequestPart(reader, prsRequest);

                this.prsController.PrsInspection(prsRequest);
              
                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.BOTTOM_ZIG)
            {
                if (Heart.Instance.CurrentVisionMode == EVisionMode.AutoRun) return;
                Heart.Instance.CurrentVisionMode = EVisionMode.Calibration;

                PrsCalibrationRequest prsRequest = new PrsCalibrationRequest
                {
                    CommonBody = commonBody
                };

                prsRequest.PrsBodies = ParsePrsCalibrationRequestPart(reader, prsRequest);

                this.calibrationController.CalculateBottomJigOffset(prsRequest);
                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.PAD_PITCH)
            {
                if (Heart.Instance.CurrentVisionMode == EVisionMode.AutoRun) return;
                Heart.Instance.CurrentVisionMode = EVisionMode.Calibration;

                PrsCalibrationRequest prsRequest = new PrsCalibrationRequest
                {
                    CommonBody = commonBody
                };

                prsRequest.PrsBodies = ParsePrsCalibrationRequestPart(reader, prsRequest);

                this.calibrationController.CalculatePickerOffset(prsRequest);
                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.X1_GRID_TABLE_CAL 
                || commonBody.InspectionType == (int)ERequestInspectionType.X2_GRID_TABLE_CAL)
            {
                if (Heart.Instance.CurrentVisionMode == EVisionMode.AutoRun) return;
                Heart.Instance.CurrentVisionMode = EVisionMode.Calibration;

                ThreePointCalibrationRequest calibrationRequest = new ThreePointCalibrationRequest
                {
                    CommonBody = commonBody,
                    TriggerType = reader.ReadUInt32(),
                    CaptureDone = reader.ReadUInt32(),
                    X1orX2 = reader.ReadUInt32(),
                };

                await this.calibrationController.CalculateVisionTableOffset(calibrationRequest);
                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.X1_TRAY_TRANSFER_CAL 
                || commonBody.InspectionType == (int)ERequestInspectionType.X2_TRAY_TRANSFER_CAL)
            {
                if (Heart.Instance.CurrentVisionMode == EVisionMode.AutoRun) return;
                Heart.Instance.CurrentVisionMode = EVisionMode.Calibration;

                ThreePointCalibrationRequest calibrationRequest = new ThreePointCalibrationRequest
                {
                    CommonBody = commonBody,
                    TriggerType = reader.ReadUInt32(),
                    CaptureDone = reader.ReadUInt32(),
                    X1orX2 = reader.ReadUInt32(),
                };

                await this.calibrationController.CalculateTrayTransferOffset(calibrationRequest);
                return;
            }

            if (commonBody.InspectionType == (int)ERequestInspectionType.TOP_BARCODE 
                || commonBody.InspectionType == (int)ERequestInspectionType.BOTTOM_BARCODE)
            {
                StripBarcodeRequest request = new StripBarcodeRequest
                {
                    CommonBody = commonBody,
                    TriggerType = reader.ReadUInt32(),
                    CaptureDone = reader.ReadUInt32(),

                };

                request.StripBody = ParseDataCodeRequestPart(reader, request);
                this.stripController.StripInspection(request);
                return;
            }
        }

        public static StripBody ParseDataCodeRequestPart(BinaryReader reader, StripBarcodeRequest codeRequest)
        {
            StripBody codeBody = new StripBody
            {
                StripBarcode = reader.ReadChars(128),
                StripCount = reader.ReadUInt32()
            };

            return codeBody;
        }

        public static MapBody? ParseMapRequestPart(BinaryReader reader, MapRequest mapRequest)
        {
            MapBody mapBody = new MapBody
            {
                StripBarcode = reader.ReadUInt32(),
                Sequence = reader.ReadUInt32(),
                GridTableNum = reader.ReadUInt32(),
                XPosition = reader.ReadUInt32(),
                YPosition = reader.ReadUInt32()
            };

            return mapBody;
        }

        public static List<EachPrsBody> ParsePrsRequestPart(BinaryReader reader, PrsRequest prsRequest)
        {
            const int swTrigger = 0;
            const int hwTrigger = 1;

            prsRequest.TriggerType = reader.ReadUInt32();
            prsRequest.CaptureDone = reader.ReadUInt32();

            List<EachPrsBody> prsList = new List<EachPrsBody>(8);
            for (int i = 0; i < 8; i++)
            {
                prsList.Add(new EachPrsBody
                {
                    StripBarcode = reader.ReadUInt32(),
                    Sequence = reader.ReadUInt32(),
                    GridTableNumber = reader.ReadUInt32(),
                    X1Orx2 = reader.ReadUInt32(),
                    ZAxisNum = reader.ReadUInt32(),
                    HasDevice = reader.ReadUInt32(),
                    XPickPosition = reader.ReadUInt32(),
                    YPickPosition = reader.ReadUInt32()
                });
            }

            return prsList;
        }

        public static List<EachPrsBody> ParsePrsCalibrationRequestPart(BinaryReader reader, PrsCalibrationRequest prsRequest)
        {
            prsRequest.InspectionResult = reader.ReadUInt32();
            prsRequest.ErrorType = reader.ReadUInt32();

            const int nPadsToRead = 8;
            List<EachPrsBody> prsList = new List<EachPrsBody>(nPadsToRead);
            for (int i = 0; i < nPadsToRead; i++)
            {
                prsList.Add(new EachPrsBody
                {
                    StripBarcode = reader.ReadUInt32(),
                    Sequence = reader.ReadUInt32(),
                    GridTableNumber = reader.ReadUInt32(),
                    X1Orx2 = reader.ReadUInt32(),
                    ZAxisNum = reader.ReadUInt32(),
                    HasDevice = reader.ReadUInt32(),
                    XPickPosition = reader.ReadUInt32(),
                    YPickPosition = reader.ReadUInt32()
                });
            }

            return prsList;
        }

        #region parse Emap
        public static List<EachEmapBody> ParseEmapBodyAsUInt32(BinaryReader reader, EmapRequest emapRequest)
        {
            try
            {
                List<EachEmapBody> emapList = new List<EachEmapBody>(128);

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    emapList.Add(new EachEmapBody
                    {
                        XPickPosition = reader.ReadUInt32(),
                        YPickPosition = reader.ReadUInt32(),
                        Data = reader.ReadUInt32(),
                        Dummy = reader.ReadUInt32()
                    });
                }

                return emapList;
            }
            catch (IOException)
            {
                throw new EmapPacketException();
            }
        }

        public static List<EachEmapBody> ParseEmapBodyAsByte(BinaryReader reader, EmapRequest emapRequest)
        {
            try
            {
                List<EachEmapBody> emapList = new List<EachEmapBody>(128);

                while (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
                {
                    emapList.Add(new EachEmapBody
                    {
                        XPickPosition = reader.ReadByte(),
                        YPickPosition = reader.ReadByte(),
                        Data = reader.ReadByte(),
                        Dummy = reader.ReadByte()
                    });
                }

                return emapList;
            }
            catch (IOException)
            {
                throw new EmapPacketException();
            }
        }
        #endregion
    }
}