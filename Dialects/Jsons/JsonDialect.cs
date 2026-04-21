using GVisionWpf.Exceptions;
using Newtonsoft.Json;
using System.IO;

namespace GVisionWpf.Dialects.Jsons
{
    public class JsonDialect : IDialect
    {
        private static readonly Lazy<JsonDialect> lazy = new Lazy<JsonDialect>(() => new JsonDialect());
        public static JsonDialect Instance => lazy.Value;

        public void Create<T>(string folderPath, string fileName, T entity)
        {
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                string json = JsonConvert.SerializeObject(entity, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                throw new JsonWriterException();
            }
        }

        public T Read<T>(string folderPath, string fileName)
        {
            string filePath = folderPath + '/' + fileName;
            if (!File.Exists(filePath))
            {
                throw new GFileNotFoundException(folderPath, fileName);
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json) ?? throw new Exception();
        }

        public void Update<T>(string folderPath, string fileName, T entity)
        {
            Delete<T>(folderPath, fileName);
            Create(folderPath, fileName, entity);
        }

        public void Delete<T>(string folderPath, string fileName)
        {
            string filePath = Path.Combine(folderPath, fileName);
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch
            {
                throw new Exception("Cannot delete file");
            }
        }

    }
}