using GVisionWpf.Exceptions;
using GVisionWpf.GlobalStates;
using GVisionWpf.Repositories;
using GVisionWpf.UIs.ViewModels;
using System.IO;

namespace GVisionWpf.Services
{
    public class RecipeService
    {
        private static readonly Lazy<RecipeService> lazy = new Lazy<RecipeService>(() => new RecipeService());
        public static RecipeService Instance => lazy.Value;
        private readonly SettingsViewModel settingsViewModel;

        private RecipeService()
        {
            this.settingsViewModel = SettingsViewModel.Instance;
        }

        public void ChangeRecipe(string recipeName)
        {
            //string recipePath = findFolder("DB/Recipes/", recipeName.Trim());

            //if (recipePath == null) { throw new VisionIfException(); }

            string fullPath = Path.Combine("DB/Recipes/", recipeName.Trim());
            
            if (!Directory.Exists(fullPath))
            {
                throw new VisionIfException();
            }

            string recipePath = fullPath;

            this.ChangeRecipe(recipePath, recipeName);

            Device currentDeviceRecipe = DeviceRecipeRepository.Instance.GetRecipe();
            if (File.Exists(GlobalSetting.Instance.DeviceInfo.VisionCurrentRecipePath))
            {
                string title = "[RECIPE]";
                string simpleName = Path.GetFileName(recipeName.Trim());
                string currentRecipe = $"name={simpleName}";
                File.WriteAllText(GlobalSetting.Instance.DeviceInfo.VisionCurrentRecipePath, $"{title}\n{currentRecipe}");
            }
        }


        public void ChangeRecipe(string recipePath, string recipeName)
        {
            GlobalSetting.Instance.DeviceInfo.RecipePath = recipePath;
            GlobalSetting.Instance.DeviceInfo.RecipeName = recipeName;

            this.settingsViewModel.SelectedRecipeName = recipeName;

            BgaRepository.Instance.InvalidateCache();
            QfnRepository.Instance.InvalidateCache();
            MoldRepository.Instance.InvalidateCache();
            LgaRepository.Instance.InvalidateCache();

            GridMoldRepository.Instance.InvalidateCache();
            GridBgaRepository.Instance.InvalidateCache();
            GridLgaRepository.Instance.InvalidateCache();
            GridQfnRepository.Instance.InvalidateCache();

            StripRepository.Instance.InvalidateCache();
            IlluminationRepository.Instance.InvalidateCache();
            DeviceRecipeRepository.Instance.InvalidateCache();

            GlobalSetting.Instance.ApplySetting();
        }

        static string findFolder(string rootDirectory, string folderNameToFind)
        {
            Queue<string> directoriesToSearch = new Queue<string>();
            directoriesToSearch.Enqueue(rootDirectory);

            while (directoriesToSearch.Count > 0)
            {
                string currentDirectory = directoriesToSearch.Dequeue();

                try
                {
                    foreach (string directory in Directory.GetDirectories(currentDirectory))
                    {
                        if (Path.GetFileName(directory).Equals(folderNameToFind, StringComparison.OrdinalIgnoreCase))
                        {
                            return directory;
                        }

                        directoriesToSearch.Enqueue(directory);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"접근 불가: {currentDirectory}");
                }
            }

            return null;
        }
    }
}
