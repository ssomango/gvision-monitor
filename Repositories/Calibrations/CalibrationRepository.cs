using GVisionWpf.Dialects;
using GVisionWpf.Dialects.Jsons;

namespace GVisionWpf.Repositories
{
    public abstract class CalibrationRepository<T>
    {
        private const string TEACHING_FOLDER_PATH = "DB/Calibration";
        protected string FileName;

        private readonly IDialect dialect = JsonDialect.Instance;

        protected CalibrationRepository(string fileName)
        {
            this.FileName = fileName;
        }

        public T GetRecipe()
        {
            return this.dialect.Read<T>(TEACHING_FOLDER_PATH, this.FileName);
        }

        public void SaveRecipe(T recipe)
        {
            this.dialect.Create(TEACHING_FOLDER_PATH, this.FileName, recipe);
        }
    }
}