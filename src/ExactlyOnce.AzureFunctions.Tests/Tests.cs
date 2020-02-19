using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample;
using Microsoft.Azure.Storage.Queue;
using NUnit.Framework;

namespace ExactlyOnce.AzureFunctions.Tests
{
    public class Tests
    {
        MessageSender sender;
        StateStore store;
        MessageReceiver receiver;

        [SetUp]
        public void SetUp()
        {
            sender = ExactlyOnceServiceCollectionExtensions.CreateMessageSender();
            store = ExactlyOnceServiceCollectionExtensions.CreateStateStore();

            var auditQueue = ExactlyOnceServiceCollectionExtensions.GetQueue("audit");
            receiver = new MessageReceiver(auditQueue);

            receiver.Start();
        }

        [TearDown]
        public Task TearDown()
        {
            return receiver.Stop();
        }

        [Test]
        public async Task DuplicatedMessages()
        {
            var gameId = Guid.NewGuid();
            var startNewRound = new StartNewRound{Id = Guid.NewGuid(), GameId = gameId, Position = 5};
            var fireAt = new FireAt{Id = Guid.NewGuid(), GameId = gameId, Position = 5};

            await SendAndWait(startNewRound);
            await SendAndWait(fireAt);
            await SendAndWait(fireAt);

            var shootingRange = Load<LeaderBoard>(gameId);
        }

        async Task SendAndWait(Message message)
        {
            var conversationId = Guid.NewGuid();

            receiver.SetupConversationTracking(conversationId, message.Id);

            await sender.Publish(
                new[] {message}, 
                new Dictionary<string, string>
                {
                    {Headers.ConversationId, conversationId.ToString() }
                });

            await receiver.WaitForConversationToFinish(conversationId);
        }

        async Task<T> Load<T>(Guid gameId)
        {
            var (state, _, __) = await store.LoadState(typeof(T), gameId, Guid.Empty);
         
            return (T)state;
        }
    }

    class MessageReceiver
    {
        CloudQueue auditQueue;
        Task task;
        CancellationTokenSource cts;

        ConcurrentDictionary<Guid, ConversationState> conversations= new ConcurrentDictionary<Guid, ConversationState>();

        public MessageReceiver(CloudQueue auditQueue)
        {
            this.auditQueue = auditQueue;
        }

        public void Start()
        {
            cts = new CancellationTokenSource();

            task = Task.Run(async () =>
            {
                while (cts.IsCancellationRequested == false)
                {
                    try
                    {
                        var message = await auditQueue.GetMessageAsync(cts.Token);

                        if (message != null)
                        {
                            var content = JsonSerializer.Deserialize<Dictionary<string, string>>(message.AsString);

                            UpdateConversations(content);
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            });
        }

        public Task Stop()
        {
            cts.Cancel();

            return task;
        }

        void UpdateConversations(Dictionary<string, string> headers)
        {
            var conversationId = Guid.Parse(headers[Headers.ConversationId]);
            var processedMessageId = Guid.Parse(headers[Headers.AuditProcessedMessageId]);
            var inflightMessageIds = headers[Headers.AuditInFlightMessageIds]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Guid.Parse)
                .ToArray();

            conversations.AddOrUpdate(
                conversationId,
                id => throw new Exception($"Failed to update conversation: {conversationId}. Make sure to call {nameof(SetupConversationTracking)} before sending initial message."),
                (id, s) => s.Update(processedMessageId, inflightMessageIds));
        }

        public Task WaitForConversationToFinish(Guid conversationId)
        {
            return conversations[conversationId].CompletionSource.Task;
        }

        public void SetupConversationTracking(Guid conversationId, Guid firstMessageId)
        {
            var added = conversations.TryAdd(conversationId, new ConversationState(firstMessageId));

            if (added == false)
            {
                throw new Exception($"Failed to setup conversation tracking for {conversationId}.");
            }
        }

        class ConversationState
        {
            Dictionary<Guid, bool> processed = new Dictionary<Guid, bool>();

            public ConversationState(Guid initialMessageId)
            {
                AddIfNotInProcessed(initialMessageId);
            }

            public ConversationState Update(Guid processedId, Guid[] inFlightIds)
            {
                AddIfNotInProcessed(processedId);

                processed[processedId] = true;

                inFlightIds.ToList().ForEach(AddIfNotInProcessed);

                if (processed.All(kv => kv.Value))
                {
                    CompletionSource.SetResult(true);
                }

                return this;
            }

            void AddIfNotInProcessed(Guid id)
            {
                if (processed.ContainsKey(id) == false)
                {
                    processed.Add(id, false);
                }
            }

            public TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();
        }
    }
}