using GVisionWpf.Dialects;
using GVisionWpf.Dialects.Jsons;
using GVisionWpf.Models.UiModels;

namespace GVisionWpf.Repositories
{
    public class WindowLayoutRepository
    {
        private static readonly Lazy<WindowLayoutRepository> lazy = new Lazy<WindowLayoutRepository>(() => new WindowLayoutRepository());
        public static WindowLayoutRepository Instance => lazy.Value;

        private const string TEACHING_FOLDER_PATH = "DB";
        const string FILE_NAME = "layout";
        const string EXTENSION = ".json";
        private readonly IDialect dialect = JsonDialect.Instance;

        private WindowLayoutRepository() { }

        public WindowLayout GetLayout(string windowName)
        {
            try
            {
                Dictionary<string, WindowLayout> dictionary = this.dialect.Read<Dictionary<string, WindowLayout>>(TEACHING_FOLDER_PATH, FILE_NAME + EXTENSION);
                return dictionary[windowName];
            }
            catch
            {
                return new WindowLayout(100, 100, 500, 500, false);
            }
        }

        public void SaveLayout(string windowName, WindowLayout layout)
        {
            try
            {
                Dictionary<string, WindowLayout> dictionary = this.dialect.Read<Dictionary<string, WindowLayout>>(TEACHING_FOLDER_PATH, FILE_NAME + EXTENSION);
                dictionary[windowName] = layout;

                this.dialect.Create(TEACHING_FOLDER_PATH, FILE_NAME + EXTENSION, dictionary);
            }
            catch
            {
                this.dialect.Create(TEACHING_FOLDER_PATH, FILE_NAME + EXTENSION, new Dictionary<string, WindowLayout>());
            }
        }
    }
}