using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Models.Dtos.Common;
using GVisionWpf.Services;
using GVisionWpf.UIs.ViewModels;
using log4net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GVisionWpf.PresentationLayer.Controllers
{
    public class RecipeController : BaseController
    {
        private static readonly Lazy<RecipeController> lazy = new Lazy<RecipeController>(() => new RecipeController());
        public static RecipeController Instance => lazy.Value;
        private readonly RecipeService recipeService;

        private static readonly ILog log = LogManager.GetLogger("Recipe");

        private RecipeController()
        {
            this.recipeService = RecipeService.Instance;
        }

        public async Task ChangeRecipe(CommonBody body)
        {
            log.Info("[Request] Change Recipe");
            GVisionMessenger.Instance.UI.SendSystemInfoMessage("Recipe Changed.");

            string[] recipeSettings;

            if (File.Exists(GlobalSetting.Instance.DeviceInfo.TrainNamePath))
            {
                recipeSettings = await File.ReadAllLinesAsync(GlobalSetting.Instance.DeviceInfo.TrainNamePath, Encoding.UTF8) ?? throw new VisionIfException();
            }
            else
            {
                throw new VisionIfException();
            }

            this.recipeService.ChangeRecipe(recipeSettings[0]);
            log.Info($"The Recipe has been changed. Recipe: {recipeSettings[0]}");
        }
    }
}
