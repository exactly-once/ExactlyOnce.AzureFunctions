using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class ShootingRange
    {
        [FunctionName(nameof(ProcessFireAt))]
        [return: Queue("attempt-updates")]
        public async Task<AttemptMade> ProcessFireAt(
            [QueueTrigger("fire-attempt")] FireAt fireAt,
            [ExactlyOnce(requestId: "{attemptId}", stateId: "{gameId}")] IOnceExecutor<ShootingRangeState> execute,
            ILogger log)
        {
            log.LogInformation($"Processed startRound: gameId={fireAt.GameId}, position={fireAt.Position}");

            var (message, blob) = await execute.Once(sr =>
            {
                var attemptMade = new AttemptMade
                {
                    AttemptId = fireAt.AttemptId,
                    GameId = fireAt.GameId
                };

                if (sr.TargetPosition == fireAt.Position)
                {
                    attemptMade.IsHit = true;
                }
                else
                {
                    attemptMade.IsHit = false;
                }

                    return (attemptMade, new BlobInfo{ BlobName = "This also a side effect" });
                });

            return message;
        }

        [FunctionName(nameof(StartNewRound))]
        public async Task StartNewRound(
            [QueueTrigger("start-round")] StartNewRound startRound,
            [ExactlyOnce(requestId: "{roundId}", stateId: "{gameId}")] IOnceExecutor<ShootingRangeState> execute,
            ILogger log)
        {
            log.LogInformation(
                $"Processed startRound: roundId={startRound.RoundId}, gameId={startRound.GameId} position={startRound.Position}");

            await execute.Once(sr =>
            {
                sr.TargetPosition = startRound.Position;
                sr.NumberOfAttempts = 0;
            });
        }

        public class ShootingRangeState : State
        {
            public int TargetPosition { get; set; }
            public int NumberOfAttempts { get; set; }
        }
    }
}