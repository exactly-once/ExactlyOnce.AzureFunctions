using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.CosmosDb;
using ExactlyOnce.AzureFunctions.Sample;
using Microsoft.Azure.Storage.Queue;
using NUnit.Framework;

namespace ExactlyOnce.AzureFunctions.Tests
{
    public class Tests
    {
        MessageSender sender;
        CosmosDbStateStore store;
        MessageReceiver receiver;

        [SetUp]
        public void SetUp()
        {
            sender = ExactlyOnceServiceCollectionExtensions.CreateMessageSender();

            store = new CosmosDbStateStore();
            store.Initialize();

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
        public async Task DuplicateFireAt()
        {
            var gameId = Guid.NewGuid();
            var startNewRound = new StartNewRound{Id = Guid.NewGuid(), GameId = gameId, Position = 5};
            var fireAt = new FireAt{Id = Guid.NewGuid(), GameId = gameId, Position = 5};

            await SendAndWait(startNewRound);
            await SendAndWait(fireAt);
            await SendAndWait(fireAt);

            var shootingRange = await Load<LeaderBoard.LeaderBoardData>(gameId);

            Assert.AreEqual(1, shootingRange.NumberOfHits);
            Assert.AreEqual(0, shootingRange.NumberOfMisses);
        }

        [Test]
        public async Task ConcurrentDuplicates()
        {
            var gameId = Guid.NewGuid();
            var startNewRound = new StartNewRound{Id = Guid.NewGuid(), GameId = gameId, Position = 5};
            var firstAttempt = new FireAt{Id = Guid.NewGuid(), GameId = gameId, Position = 5};
            var secondAttempt = new FireAt{Id = Guid.NewGuid(), GameId = gameId, Position = 5};

            await SendAndWait(startNewRound);

            var firstAttemptNo = 100;
            var secondAttemptNo = 120;

            var tasks = Enumerable.Range(1, firstAttemptNo).Select(_ => Send(firstAttempt))
                 .Union(Enumerable.Range(1, secondAttemptNo).Select(_ => Send(secondAttempt))).ToArray();

            await Task.WhenAll(tasks);

            var shootingRange = await Load<LeaderBoard.LeaderBoardData>(
                gameId,
                s => s.NumberOfHits == 2,
                10000);

            Assert.AreEqual(2, shootingRange.NumberOfHits);
            Assert.AreEqual(0, shootingRange.NumberOfMisses);
        }

        [Test]
        public async Task DuplicateStartNewRound()
        {
            var gameId = Guid.NewGuid();
            var startFirstRound = new StartNewRound{Id = Guid.NewGuid(), GameId = gameId, Position = 5};
            var fireAt = new FireAt{Id = Guid.NewGuid(), GameId = gameId, Position = 7};
            var startSecondRound = new StartNewRound{Id = Guid.NewGuid(), GameId = gameId, Position = 10};

            await SendAndWait(startFirstRound);
            await SendAndWait(startSecondRound);
            await SendAndWait(fireAt);
            await SendAndWait(startFirstRound);

            var shootingRange = await Load<ShootingRange.ShootingRangeData>(gameId);

            Assert.AreEqual(10, shootingRange.TargetPosition);
            Assert.AreEqual(1, shootingRange.NumberOfAttempts);
        }

        async Task SendAndWait(Message message)
        {
            await await Send(message);
        }

        async Task<Task> Send(Message message)
        {
            var conversationId = Guid.NewGuid();

            receiver.SetupConversationTracking(conversationId, message.Id);

            await sender.Publish(
                new[] {message},
                new Dictionary<string, string>
                {
                    {Headers.ConversationId, conversationId.ToString()}
                });

            var waitForFinishTask = receiver.WaitForConversationToFinish(conversationId);
            
            return waitForFinishTask;
        }

        async Task<T> Load<T>(Guid itemId, Func<T, bool> precondition = null, int timeoutInMs = 5000) where T : class
        {
            //HINT: We don't have default session consistency guarantee here (writes are done from the host).
            //      precondition enables waiting until consistent prefix catches up.
            //      This is not fully safe though as we don't know if there is no more state changes happening.

            precondition = precondition ?? ((_) => true);

            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutInMs)
            {
                var item = await store.Load(itemId, typeof(T));

                if (item?.Item is T state && precondition(state))
                {
                    return state;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            throw new AssertionException($"Load precondition failure for state {itemId}.");
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
                    catch (Exception e)
                    {
                        TestContext.WriteLine($"Receiver exception {e.Message}");
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

            if (conversations.ContainsKey(conversationId) == false)
            {
                return;
            }

            conversations.AddOrUpdate(
                conversationId,
                id => throw new Exception("Invalid conversation tracking state."),
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