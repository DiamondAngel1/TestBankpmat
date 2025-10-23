using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyClient.JSON_Converter
{
    public abstract class RequestBase
    {
        public abstract string Type { get; } // Abstract property to get the type of request
    }
    public class RequestBaseConverter : JsonConverter<RequestBase>
    {
        public override RequestBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            string type = root.GetProperty("Type").ToString();

            return type switch
            {
                "MESSAGE" => JsonSerializer.Deserialize<RequestResponseMessage>(root.GetRawText(), options),
                "CARD_CHECK" => JsonSerializer.Deserialize<RequestCardCheck>(root.GetRawText(), options),
                "AUTH_OR_REGISTER" => JsonSerializer.Deserialize<RequestAuthOrReg>(root.GetRawText(), options),
                "DEPOSIT" => JsonSerializer.Deserialize<RequestDeposit>(root.GetRawText(), options),
                "WITHDRAW" => JsonSerializer.Deserialize<RequestWithdraw>(root.GetRawText(), options),
                "VIEW_BALANCE" => JsonSerializer.Deserialize<RequestViewBalance>(root.GetRawText(), options),
                _ => throw new NotSupportedException($"Unknown request type: {type}")
            };

        }
        

        public override void Write(Utf8JsonWriter writer, RequestBase value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
