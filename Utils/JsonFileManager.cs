using Newtonsoft.Json;
using System.IO;
using System.Windows;

namespace GVisionWpf.Utils
{
    // LEGACY
    class JsonFileManager
    {
        public static List<string> GetFilePathsInTargetDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory, "*.json");
            return fileEntries.ToList();
        }

        public static void SaveToJson<T>(string filePath, T objectToSave)
        {
            try
            {
                string json = JsonConvert.SerializeObject(objectToSave, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                throw new JsonWriterException();
            }
        }

        public static T LoadFromJson<T>(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading from JSON: " + ex.Message);
                throw new FileNotFoundException();
            }
        }
    }
}
