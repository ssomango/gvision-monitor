using GVisionWpf.Repositories;

namespace GVisionWpf.Services
{
    public class IlluminationService
    {
        private static readonly Lazy<IlluminationService> lazy = new Lazy<IlluminationService>(() => new IlluminationService());
        public static IlluminationService Instance => lazy.Value;

        private readonly IlluminationRepository illuminationRepository;

        private IlluminationService()
        {
            this.illuminationRepository = IlluminationRepository.Instance;
        }

        public void SaveRecipe(IlluminationRecipe illuminationRecipe)
        {
            this.illuminationRepository.SaveRecipe(illuminationRecipe);
        }

        public IlluminationRecipe GetIlluminationRecipe()
        {
            return this.illuminationRepository.GetRecipe();
        }

        public int GetShotCount(ECamera camera)
        {
            return this.illuminationRepository.GetRecipe().Setting[camera].Count;
        }
    }
}