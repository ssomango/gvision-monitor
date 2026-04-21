namespace GVisionWpf.Repositories
{
    public class DeviceRecipeRepository : RecipeRepository<Device>
    {
        private static readonly Lazy<DeviceRecipeRepository> lazy = new Lazy<DeviceRecipeRepository>(() => new DeviceRecipeRepository());
        public static DeviceRecipeRepository Instance => lazy.Value;

        private DeviceRecipeRepository() : base("DEVICE_INFO.dev") { }

    }
}
