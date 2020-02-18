using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        async Task SendAndWait(Message startNewRound)
        {
            await sender.Publish(new[] {startNewRound});

            await receiver.WaitForConversationToFinish(startNewRound.Id);
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
                    var message = await auditQueue.GetMessageAsync(cts.Token);

                    if (message != null)
                    {
                        var content = JsonSerializer.Deserialize<Dictionary<string, string>>(message.AsString);

                        UpdateConversations(content);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
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
            var messageDelta = int.Parse(headers[Headers.AuditMessageDelta]);

            conversations.AddOrUpdate(
                conversationId,
                id => new ConversationState
                {
                    InFlightMessages = 1 + messageDelta,
                    CompletionSource = new TaskCompletionSource<bool>()
                },
                (id, s) =>
                {
                    s.InFlightMessages += messageDelta;

                    if (s.InFlightMessages == 0)
                    {
                        s.CompletionSource.SetResult(true);
                    }

                    return s;
                });
        }

        public Task WaitForConversationToFinish(Guid conversationId)
        {
            var conversationState = conversations.AddOrUpdate(
                conversationId,
                id => new ConversationState
                {
                    InFlightMessages = 1,
                    CompletionSource = new TaskCompletionSource<bool>()
                },
                (id, state) => state);

            return conversationState.CompletionSource.Task;
        }

        class ConversationState
        {
            public int InFlightMessages { get; set; }

            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}