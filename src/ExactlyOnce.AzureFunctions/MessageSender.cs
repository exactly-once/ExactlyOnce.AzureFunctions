using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    public class MessageSender
    {
        QueueProvider queueProvider;
        RoutingConfiguration configuration;

        public MessageSender(QueueProvider queueProvider, RoutingConfiguration configuration)
        {
            this.queueProvider = queueProvider;
            this.configuration = configuration;
        }

        public Task Publish(Guid messageId, object message, Dictionary<string, string> headers = null)
        {
            headers ??= new Dictionary<string, string>();

            var messageBytes = MessageSerializer.ToBytes(messageId, headers, message);

            var queueMessage = new CloudQueueMessage(messageBytes);

            var destinationQueue = MapMessageToQueue(message.GetType());

            return MapMessageToQueue(message.GetType()).AddMessageAsync(queueMessage);
        }

        CloudQueue MapMessageToQueue(Type type)
        {
            var queueName = configuration.Routes[type];

            return queueProvider.GetQueue(queueName);
        }
    }
}