using GVisionWpf.Exceptions;
using Newtonsoft.Json;
using System.IO;

namespace GVisionWpf.Dialects.Jsons
{
    public class CacheJsonDialect : IDialect
    {
        private static readonly Lazy<CacheJsonDialect> lazy = new Lazy<CacheJsonDialect>(() => new CacheJsonDialect());
        public static CacheJsonDialect Instance => lazy.Value;

        private static readonly Dictionary<string, string> jsonCache = new Dictionary<string, string>();
        private static readonly object cacheLock = new object();

        public void Create<T>(string folderPath, string fileName, T entity)
        {
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                string json = JsonConvert.SerializeObject(entity, Formatting.Indented);
                File.WriteAllText(filePath, json);

                lock (cacheLock)
                {
                    jsonCache[filePath] = json;
                }
            }
            catch
            {
                throw new JsonWriterException();
            }
        }

        public T Read<T>(string folderPath, string fileName)
        {
            string filePath = Path.Combine(folderPath, fileName);
            if (!File.Exists(filePath))
            {
                throw new GFileNotFoundException(folderPath, fileName);
            }

            lock (cacheLock)
            {
                if (jsonCache.ContainsKey(filePath))
                {
                    return JsonConvert.DeserializeObject<T>(jsonCache[filePath]) ?? throw new Exception("Deserialization failed");
                }
                else
                {
                    string json = File.ReadAllText(filePath);
                    jsonCache[filePath] = json;
                    return JsonConvert.DeserializeObject<T>(json) ?? throw new Exception("Deserialization failed");
                }
            }
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

                lock (cacheLock)
                {
                    jsonCache.Remove(filePath);
                }
            }
            catch
            {
                throw new Exception("Cannot delete file");
            }
        }

        public void InvalidateCache(string folderPath, string fileName)
        {
            string filePath = Path.Combine(folderPath, fileName);

            lock (cacheLock)
            {
                jsonCache.Remove(filePath);
            }
        }
    }

}