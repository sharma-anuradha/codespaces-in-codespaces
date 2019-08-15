using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    public class BillingEventArgsConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = (JToken)serializer.Deserialize(reader);

            if (token is JObject obj)
            {
                // This converter may support other event args types in the future
                // by checking for key properties of those types.

                if (obj.ContainsKey("oldValue") && obj.ContainsKey("newValue"))
                {
                    return obj.ToObject<BillingStateChange>();
                }
                else if (obj.ContainsKey("periodStart") && obj.ContainsKey("periodEnd") &&
                    obj.ContainsKey("usage"))
                {
                    return obj.ToObject<BillingSummary>();
                }
            }

            return token;
        }

        public override bool CanConvert(Type objectType) => throw new NotSupportedException();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}
