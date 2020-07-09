## Overview

This repository is sample of the exactly-once processing in AzureFunctions environment.  

## Consistent output

The sample shows how to leverage exactly-once with using CosmosDB as a storage system with communication over HTTP and Azure Storage Queues. 

At the code level processing logic gets wrapped into lambda passed to `IOnceExecutor<TState>`. The executor ensures consistent output - for a given combination of `requestId`, `stateId` and `TState` values the `Once` method will return exaclty same output. In the sample below for each `attemptId` and `gameId` the `Once` method will always return identical output.

``` csharp
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

        return (attemptMade, new BlobInfo {BlobName = "This also a side effect"});
    });

    return message;
}
```

## State

State that is changed inside the function gets stored in CosmosDB. It has to inherit from `State` object that defines obligatory `id` and `_transactionId` fields. 

``` csharp

public abstract class State
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("_transactionId")] public Guid? TxId { get; set; }
}
    
public class ShootingRangeState : State
{
    public int TargetPosition { get; set; }
    public int NumberOfAttempts { get; set; }
}
```

All intrastructural data used for ensuring the once behavior are stored in a separte collection.

## Configuration

Library requires `CosmosClient` and `IStateStore` implementation to be registerd the DI container. In addition, it requires outbox configuration.

``` csharp
var client = new CosmosClient(endpointUri, primaryKey);

builder.Services.AddSingleton(sp => client);
builder.Services.AddSingleton(sp => new CosmosDbStateStore(client, databaseId));

builder.AddExactlyOnce(c =>
{
    c.ConfigureOutbox(o =>
    {
        o.DatabaseId = databaseId;
        o.ContainerId = "Outbox";
        o.RetentionPeriod = TimeSpan.FromSeconds(30);
    });

    c.StateStoreIs<CosmosDbStateStore>();
});
```
