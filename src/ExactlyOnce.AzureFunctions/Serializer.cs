using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace ExactlyOnce.AzureFunctions
{
    public static class Serializer {

        public const string MessageTypeName = "MessageType";

        public static byte[] Serialize(object message, Dictionary<string, string> headers)
        {
            var text = TextSerialize(message, headers);

            return Encoding.UTF8.GetBytes(text);
        }

        public static string TextSerialize(object message, Dictionary<string, string> headers)
        {
            headers.Add(MessageTypeName, message.GetType().AssemblyQualifiedName);

            var jsonObject = JObject.FromObject(message);
            jsonObject.Add("headers", JObject.FromObject(headers));

            var text = jsonObject.ToString();

            return text;
        }

        public static (Dictionary<string, string>, object) Deserialize(byte[] body)
        {
            var text = Encoding.UTF8.GetString(body);

            return TextDeserialize(text);
        }

        public static (Dictionary<string, string>, object) TextDeserialize(string text)
        {
            var jsonObject = JObject.Parse(text);
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonObject["headers"].ToString());

            var messageType = headers[Serializer.MessageTypeName];
            var message = JsonSerializer.Deserialize(text, Type.GetType(messageType));

            return (headers, message);
        }
    }
}