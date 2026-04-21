using Newtonsoft.Json;

namespace GVisionWpf.Utils
{
    public class DeepCopy
    {
        public static T Copy<T>(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null");
            }

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            };


            var serializedObject = JsonConvert.SerializeObject(obj, settings);
            return JsonConvert.DeserializeObject<T>(serializedObject, settings);
        }
    }
}
