using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace ExactlyOnce.AzureFunctions
{
    public static class MessageSerializer {
        
        const string HeadersPropertyName = "_headers";

        public static byte[] ToBytes(Guid id, Dictionary<string, string> headers, object message)
        {
            var text = ToJson(id, headers, message);

            return Encoding.UTF8.GetBytes(text);
        }

        public static string ToJson(Guid id, Dictionary<string, string> headers, object message)
        {
            headers.Add(Headers.MessageId, id.ToString());
            headers.Add(Headers.MessageType, message.GetType().AssemblyQualifiedName);

            var jsonObject = JObject.FromObject(message); ;
            jsonObject.Add(HeadersPropertyName, JObject.FromObject(headers));

            var text = jsonObject.ToString();

            return text;
        }

        public static (Dictionary<string, string>, object) FromBytes(byte[] body)
        {
            var text = Encoding.UTF8.GetString(body);

            return FromJson(text);
        }

        public static (Dictionary<string, string>, object) FromJson(string text)
        {
            var jsonObject = JObject.Parse(text);
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonObject[HeadersPropertyName].ToString());

            var messageType = headers[Headers.MessageType];
            var message = JsonSerializer.Deserialize(text, Type.GetType(messageType));

            return (headers, message);
        }
    }
}