using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class ShootingRange
    {
        IOnceExecutor executor;

        public ShootingRange(IOnceExecutor executor)
        {
            this.executor = executor;
        }

        [FunctionName(nameof(ProcessFireAt))]
        [return: Queue("attempt-updates")]
        public async Task<AttemptMade> ProcessFireAt([QueueTrigger("fire-attempt", Connection = "AzureWebJobsStorage")]
            FireAt fireAt,
            ILogger log)
        {
            log.LogInformation($"Processed startRound: gameId={fireAt.GameId}, position={fireAt.Position}");

            var output = await executor.Once<FireAt>(fireAt.AttemptId)
                .On<ShootingRangeState>(fireAt.GameId, sr =>
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

                    return new SendMessage<AttemptMade>(attemptMade);
                });

            return ((SendMessage<AttemptMade>) output).Message;
        }

        [FunctionName(nameof(StartNewRound))]
        public async Task StartNewRound([QueueTrigger("start-round", Connection = "AzureWebJobsStorage")]
            StartNewRound startRound,
            ILogger log)
        {
            log.LogInformation(
                $"Processed startRound: roundId={startRound.RoundId}, gameId={startRound.GameId} position={startRound.Position}");

            await executor.Once<StartNewRound>(startRound.RoundId)
                .On<ShootingRangeState>(startRound.GameId, sr =>
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