## Overview

This repository shows how to use exactly-once processing in AzureFunctions environment.  

## Consistent output

The sample shows how to leverage exactly-once with state stored in CosmosDB and communication over HTTP and Azure Storage Queues. 

At the code level processing logic gets wrapped into lambda passed to the exaclty-once library. The library makes consistent output ie. exaclty the same for duplicated and out-of-order executions. For example, in the sinppet below `(message, blog)` tuple will be consistently returned:

``` csharp
var (message, blob) = await execute
      .Once<FireAt>(fireAt.AttemptId)
      .On<ShootingRangeState>(fireAt.GameId)
      .WithOutput(sr =>
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

          return (attemptMade, new BlobInfo{BlobName = "This also a side effect"});
      });

```

## POCO state

State that is change by the functions and stored in CosmosDB is a POCO with obligatory `id` field `_transactionId`. All other intrastructural data is stored in a separate collection.

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

### Configuration

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
