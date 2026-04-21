using Newtonsoft.Json;
using System.Reflection;

namespace GVisionWpf.Utils
{
    public class ToStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            MethodInfo parse = objectType.GetMethod("Parse", new Type[] { typeof(string) });
            if (parse != null && parse.IsStatic && parse.ReturnType == objectType)
            {
                return parse.Invoke(null, new object[] { (string)reader.Value });
            }

            throw new JsonException(string.Format(
                "The {0} type does not have a public static Parse(string) method that returns a {0}.",
                objectType.Name));
        }
    }
}
