using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class LeaderBoard
    {
        IOnceExecutor execute;

        public LeaderBoard(IOnceExecutor execute)
        {
            this.execute = execute;
        }

        [FunctionName(nameof(UpdateLeaderBoard))]
        public void UpdateLeaderBoard([QueueTrigger("attempt-updates")]
            AttemptMade attempt, ILogger log)
        {
            log.LogInformation($"Processing attempt result: gameId={attempt.GameId}, isHit={attempt.IsHit}");

            execute.Once<AttemptMade>(attempt.AttemptId)
                .On<LeaderBoardState>(attempt.GameId, board =>
                {
                    board.NumberOfAttempts++;

                    if (attempt.IsHit)
                    {
                        board.NumberOfHits++;
                    }
                });
        }

        public class LeaderBoardState : State
        {
            [JsonProperty("numberOfAttempts")] public int NumberOfAttempts { get; set; }

            [JsonProperty("numberOfHits")] public int NumberOfHits { get; set; }
        }
    }
}