using GVisionWpf.Exceptions;
using GVisionWpf.Illuminations.Serials;
using GVisionWpf.Services;

namespace GVisionWpf.Illuminations
{
    public class LightManager
    {
        private static readonly Lazy<LightManager> Lazy = new Lazy<LightManager>(() => new LightManager());
        public static LightManager Instance => Lazy.Value;

        // 카메라별 조명 정리 
        public Dictionary<ECamera, Dictionary<ELight, Light>> Lights = new Dictionary<ECamera, Dictionary<ELight, Light>>();

        //시리얼 포트로 조명을 제어하는 드라이버 모음
        public Dictionary<string, ILightSerial> LightSerials = new Dictionary<string, ILightSerial>();


        private LightManager()
        {
            try
            {
                //시리얼 연결
                this.LightSerials = LightLoader.LoadLightSerials();

            }
            catch
            {
                throw new NotSupportedSerialException();
            }

            // 조명 셋팅 
            this.Lights = LightLoader.LoadLightSettings(this.LightSerials);
        }

        public void SetBrightness(ECamera cameraType, Dictionary<ELight, int> lights) //여러 조명 밝기 한번에 설정
        {
            foreach (var (lightType, brightness) in lights)
            {
                this.Lights[cameraType][lightType].SetBrightness(brightness);
            }
        }


        public void TurnOnLight(ECamera cameraType) //레시피 기반 조명 켜기, 밝기 조정이 조명을 켜는 방식인 듯
        {
            IlluminationRecipe illuminationRecipe = IlluminationService.Instance.GetIlluminationRecipe();

            if (!illuminationRecipe.Setting.ContainsKey(cameraType))
            {
                throw new NoIlluminationException();
            }

            var recipe = illuminationRecipe.Setting[cameraType];

            if (recipe.Count == 0)
            {
                return;
            }

            this.SetBrightness(cameraType, recipe[0]);
        }


        public void TurnOffAllLights(ECamera cameraType) // 조명 끔
        {
            if (!this.Lights.ContainsKey(cameraType))
            {
                return;
            }

            foreach (var (lightType, light) in this.Lights[cameraType])
            {
                light.TurnOff();
            }
        }

        public void SetBrightness(ECamera cameraType, ELight lightType, int brightness) // 특정 조명 밝기 설정 
        {
            this.Lights[cameraType][lightType].SetBrightness(brightness);
        }

        public void TurnOffAllLightsFromAllCamera() //모든 카메라 조명 끔
        {
            foreach (ECamera cameraType in Enum.GetValues(typeof(ECamera)))
            {
                TurnOffAllLights(cameraType);
            }
        }
        public void Save()
        {

        }
    }
}
