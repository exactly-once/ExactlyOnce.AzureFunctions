using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public static class Serializer {

        public const string MessageTypeName = "MessageType";

        internal static byte[] Serialize<T>(T message, Dictionary<string, string> headers)
        {
            headers.Add(MessageTypeName, message.GetType().FullName);

            var envelope = new Envelope
            {
                Headers = headers,
                Content = JsonSerializer.Serialize(message)
            };

            var text = JsonSerializer.Serialize(envelope);

            return Encoding.UTF8.GetBytes(text);
        }

        internal static (Dictionary<string, string>, object) Deserialize(byte[] body)
        {
            var text = Encoding.UTF8.GetString(body);
            var envelope = JsonSerializer.Deserialize<Envelope>(text);

            var messageType = envelope.Headers[Serializer.MessageTypeName];
            var message = JsonSerializer.Deserialize(envelope.Content, Type.GetType(messageType));

            return (envelope.Headers, message);
        }
    }
}