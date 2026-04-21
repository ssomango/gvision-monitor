using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Illuminations.Lights;
using GVisionWpf.Illuminations.Serials;
using GVisionWpf.Services;

namespace GVisionWpf.Illuminations
{
    public class LightLoader
    {

        /*
         
            1	GlobalSetting.Instance.LightInfos로 카메라별 조명 리스트를 읽음
            2	IlluminationService.Instance.GetIlluminationRecipe()에서 밝기 값도 읽음
            3	Light 타입(예: Ds16Light, LvsEt04Light)에 맞춰 Light 객체 생성
            4	카메라별로 Dictionary<ELight, Light>를 채워서 리턴 

         */
        public static Dictionary<ECamera, Dictionary<ELight, Light>> LoadLightSettings(Dictionary<string, ILightSerial> LightSerials)
        {
            Dictionary<ECamera, Dictionary<ELight, Light>> CameraLights = new Dictionary<ECamera, Dictionary<ELight, Light>>();

            var cameraSettingsList = GlobalSetting.Instance.LightInfos;

            IlluminationRecipe recipe = IlluminationService.Instance.GetIlluminationRecipe();

            foreach (var cameraSetting in cameraSettingsList)
            {
                ECamera cameraEnumType = cameraSetting.CameraType;
                Dictionary<ELight, Light> Lights = new Dictionary<ELight, Light>();

                foreach (var lightSetting in cameraSetting.Lights)
                {
                    ELight lightEnumType = lightSetting.LightType;
                    string controllerName = lightSetting.ControllerName;
                    string lightName = lightSetting.LightName;
                    int channel = lightSetting.Channel;
                    // int brightness = lightSetting.Brightness;
                    int brightness = recipe.Setting[cameraEnumType].First()[lightEnumType];
                    int maxBrightness = lightSetting.MaxBrightness;
                    bool isInterlocked = lightSetting.IsInterlocked;
                    string interlockGroup = lightSetting.InterlockGroup;

                    if (LightSerials.ContainsKey(controllerName))
                    {
                        ILightSerial lightSerial = LightSerials[controllerName];

                        if (lightSerial is IDs16Serial ds16Serial)
                        {
                            byte binaryChannel = Convert.ToByte(channel);
                            Lights[lightEnumType] = new Ds16Light(ds16Serial, lightName, binaryChannel, brightness, maxBrightness, isInterlocked, interlockGroup);
                        }
                        else if (lightSerial is ILvsEt04Serial et04Serial)
                        {
                            byte binaryChannel = Convert.ToByte(channel);
                            Lights[lightEnumType] = new LvsEt04Light(et04Serial, lightName, binaryChannel, brightness, maxBrightness, isInterlocked, interlockGroup);
                        }
                        else if (lightSerial is ILvsEn08Serial en08Serial)
                        {
                            byte binaryChannel = Convert.ToByte(channel);
                            Lights[lightEnumType] = new LvsEn08Light(en08Serial, lightName, binaryChannel, brightness, maxBrightness, isInterlocked, interlockGroup);
                        }
                        else if (lightSerial is IKv600Serial kv600Serial)
                        {
                            byte binaryChannel = Convert.ToByte(channel);
                            Lights[lightEnumType] = new Kv600Light(kv600Serial, lightName, binaryChannel, brightness, maxBrightness, isInterlocked, interlockGroup);
                        }
                        else if (lightSerial is ILFineSerial lFineSerial)
                        {
                            byte binaryChannel = Convert.ToByte(channel);
                            Lights[lightEnumType] = new LFineLight(lFineSerial, lightName, binaryChannel, brightness, maxBrightness, isInterlocked, interlockGroup);
                        }
                        else
                        {
                            throw new UnknownLightControllerException(controllerName);
                        }
                    }
                    else
                    {
                        throw new UnknownLightControllerException(controllerName);
                    }
                }

                CameraLights[cameraEnumType] = Lights;
            }

            return CameraLights;

        }


        /*
         
            1	GlobalSetting.Instance.LightControllerInfos에서 전체 조명 컨트롤러 설정을 읽음
            2	ELightInterface 타입별로 포트 오픈 (Ds16Serial, LvsEn08Serial, LvsEt04Serial, Kv600Serial, LFineSerial)
            3	포트 통신 객체를 Dictionary에 추가 
        
         */

        public static Dictionary<string, ILightSerial> LoadLightSerials()
        {
            Dictionary<string, ILightSerial> LightSerials = new Dictionary<string, ILightSerial>();

            var lightControllerList = GlobalSetting.Instance.LightControllerInfos;


            foreach (var lightControllerSetting in lightControllerList)
            {
                ELightInterface lightInterface = lightControllerSetting.LightInterface;
                string controllerName = lightControllerSetting.ControllerName;
                string comPort = lightControllerSetting.ComPort;
                int baudRate = lightControllerSetting.BaudRate;


                switch (lightInterface)
                {
                    case ELightInterface.Ds1624:
                        Ds16Serial ds16Serial = new Ds16Serial(comPort, baudRate);
                        LightSerials.Add(controllerName, ds16Serial);
                        break;
                    case ELightInterface.En08:
                        LvsEn08Serial en08Serial = new LvsEn08Serial(comPort, baudRate);
                        LightSerials.Add(controllerName, en08Serial);
                        break;
                    case ELightInterface.Et04:
                        LvsEt04Serial et04Serial = new LvsEt04Serial(comPort, baudRate);
                        LightSerials.Add(controllerName, et04Serial);
                        break;
                    case ELightInterface.Kv600:
                        Kv600Serial kv600Serial = new Kv600Serial(comPort, baudRate);
                        LightSerials.Add(controllerName, kv600Serial);
                        break;
                    case ELightInterface.LFine:
                        LFineSerial lFineSerial = new LFineSerial(comPort, baudRate);
                        LightSerials.Add(controllerName, lFineSerial);
                        break;
                    default:
                        throw new UnknownLightControllerException(controllerName);
                        break;
                }
            }

            return LightSerials;
        }


    }
}
