using GVisionWpf.Dialects.Jsons;
using GVisionWpf.GlobalStates;
using System.IO;

namespace GVisionWpf.Repositories
{
    public abstract class RecipeRepository<T>
    {
        protected string fileName;


        /*
         * INTENTION:
         * File IO의 Load를 줄이기 위해 IO만 캐싱하고 있습니다.
         * 이 레시피에서 Recipe 객체를 캐싱할 경우, Recipe 내부의 레퍼런스 타입의 변화하면 캐싱된 Recpie 객체에도 영향을 받게 됩니다. 그렇다는 말은 그 캐시는 더이상 valid하지 않다.
         * 예를 들어. Diff 연산 이슈 등, 그래서 Dialect에서 꼭 File IO의 io 작업만 캐싱하도록 하세요.
         *
         * 설정에서 레시피를 변경한 경우, 레시피의 캐시를 Invalidate 해야합니다. 따로 TTL 없음 (일부러 안넣음 꼭 정해진 대로만 돌도록)
         */
        protected readonly CacheJsonDialect dialect = CacheJsonDialect.Instance;

        public string RecipeFolderPath
        {
            get => GlobalSetting.Instance.DeviceInfo.RecipePath;
        }

        public RecipeRepository(string fileName)
        {
            this.fileName = fileName;
        }

        public T GetRecipe()
        {
            return this.dialect.Read<T>(RecipeFolderPath, this.fileName);
        }

        public void SaveRecipe(T teaching)
        {
            this.dialect.Create(RecipeFolderPath, this.fileName, teaching);
        }

        public void SaveRecipeByPath(T teaching, string path)
        {
            this.dialect.Create(path, this.fileName, teaching);
        }

        public bool DoesTeachingExist()
        {
            return File.Exists(RecipeFolderPath + this.fileName);
        }

        public void InvalidateCache()
        {
            this.dialect.InvalidateCache(RecipeFolderPath, this.fileName);
        }
    }
}