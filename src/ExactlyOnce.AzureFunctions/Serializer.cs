using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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

            var envelope = new Envelope
            {
                Headers = headers,
                Content = JsonSerializer.Serialize(message, message.GetType())
            };

            var text = JsonSerializer.Serialize(envelope);
            return text;
        }

        public static (Dictionary<string, string>, object) Deserialize(byte[] body)
        {
            var text = Encoding.UTF8.GetString(body);

            return TextDeserialize(text);
        }

        public static (Dictionary<string, string>, object) TextDeserialize(string text)
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(text);

            var messageType = envelope.Headers[Serializer.MessageTypeName];
            var message = JsonSerializer.Deserialize(envelope.Content, Type.GetType(messageType));

            return (envelope.Headers, message);
        }
    }
}