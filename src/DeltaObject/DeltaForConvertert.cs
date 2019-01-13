using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeltaObject
{
    public class DeltaForConvertert : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DeltaFor<>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = Activator.CreateInstance(objectType) as dynamic;
            var jObject = JObject.Load(reader);
            foreach (var prop in jObject)
            {
                result.SetProperty(prop.Key, jObject[prop.Key]);
            }
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}