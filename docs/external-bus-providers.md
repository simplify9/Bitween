# External bus providers

A `BusGateway` can now be fed by an external broker instead of the internal bus, through a
resident serverless adapter.

**Nothing existing changes.** `BusGateway.DataSourceId` is nullable and null still means the
internal bus, so every gateway already in a database behaves exactly as before. The migration
(`ExternalBusDataSources`) is additive: three columns and one table.

## The shape

```
external broker  ->  resident adapter        (owns the connection, one process)
                 ->  BusProviderEventSink    (resolves the gateway, persists the Xchange)
                 ->  XchangeService.SubmitFilterXchange
                 ->  filter -> mapper -> handler -> auto-retry     (unchanged)
                 ->  ack returns  ->  adapter acknowledges its broker
```

Past the sink, ingress from a broker and ingress from the API are the same thing. Filtering,
mapping, work-group routing and the audit trail are not reimplemented.

**The adapter does not acknowledge its broker until Bitween has persisted.** A rejection means
requeue, not loss — a Bitween outage stops draining the customer's queue rather than dropping
their messages. A crash between persisting and acknowledging means redelivery, which is why every
event carries a dedupe key.

## What lives where

| | |
|---|---|
| `DataSource` | How to reach the system: endpoint, credentials, health. One per broker. |
| `BusGateway.DataSourceId` | Which broker feeds this gateway. Null = internal bus. |
| `BusGateway.Endpoint` | Which queue or topic on it. |
| `BusGateway.EndpointProperties` | Per-subscription overrides: prefetch, visibility timeout. |
| `BusGateway.DocumentId` + routes | Unchanged — what the message *means* and what runs. |

One data source serves many gateways, exactly as one connection serves many queues.

## Turning it on

```json
"Bitween": { "BusProvidersEnabled": true, "BusProviderMaxInFlight": 16 }
```

**Safe on every node.** Each data source is owned through a lease, so exactly one node consumes it
and the rest stand by. Still opt-in, but for a different reason than before: it opens outbound
connections to third-party brokers, and that should be a decision rather than a default.

## Placement: the bus provides the lock, the database provides the fence

Two nodes consuming one queue is duplicate processing — the failure this whole design exists to
prevent. So ownership is granted per data source, not globally: whichever node wins each race owns
that source, so connection load spreads without anyone scheduling it, and one node leaving does not
move everything at once.

**The lock** is a RabbitMQ exclusive queue, `bitween.lease.datasource.{id}`. An exclusive queue
belongs to a single connection, so declaring it succeeds for exactly one node and fails for every
other — and the broker releases it the moment that connection dies. Liveness for free: no lease
renewal to get wrong, no clock to trust.

**The fence** is a monotonic `term` in `cluster_lease`, because the lock alone is not enough. A
node can be paused long enough — a stop-the-world GC, a partition that heals — for its queue to be
released and reclaimed while it still believes it owns the resource, and RabbitMQ has no counter
that would reveal it. Acquiring bumps the term; a holder whose term is no longer current has been
superseded and stops immediately.

Three details that are load-bearing:

* **Recovery is off on the election connection.** A recovered connection silently re-declares the
  exclusive queue, so a node that lost ownership during an outage would take it back *without
  bumping the term* — two owners, neither aware.
* **Releasing DELETES the queue.** An exclusive queue belongs to the connection, not the channel,
  so closing the channel releases nothing. Without the delete, mutual exclusion and crash failover
  both work while GRACEFUL handover silently never completes — ownership sits with a shut-down
  node until its connection finally drops, which is precisely what a rolling restart does.
* **Losing a lease stops the adapter WITHOUT draining.** Another node may already be consuming, so
  finishing in-flight work risks handling the same messages twice.

`ILeaderElection` exists so the mechanism can be replaced when the internal bus is no longer
RabbitMQ — not so two implementations can be maintained at once.

## Not done yet

## The two providers

### `SW.Bitween.Adapters.Bus.RabbitMq`

An external RabbitMQ — someone else's broker, not Bitween's own. Consumes with `autoAck: false`,
acks only after Bitween persists, nacks with requeue on rejection. `DeclareMode` is `none`,
`assert` (passive declare, fail loudly) or `create`; `assert` is the default because silently
creating queues on a customer's broker is not our call.

Dedupe key is the broker's message id, or a content hash — **not** the delivery tag, which is per
channel and restarts at 1 on every reconnect.

Also supports `Publish`, so egress works on external gateways even though the internal one does
not have it yet.

### `SW.Bitween.Adapters.Bus.Sqs`

SQS is polled, not pushed, and has no ack — only delete. Same contract by a different mechanism:

```
receive -> persist -> ONLY THEN DeleteMessage
```

A rejection resets visibility to 0 so it retries in seconds rather than waiting out the timeout.
**`VisibilityTimeoutSeconds` must exceed how long Bitween takes to persist**, or a message is
redelivered while the first copy is still being handled — `TestConnection` warns below ~30s.

Credentials are optional: leave them blank to use the ambient chain (instance profile, IRSA,
environment), which is the right answer on AWS.

**Why this one matters beyond SQS itself:** the Amazon Selling Partner API delivers notifications
by publishing to an SQS queue *you* own — you create the queue, grant SP-API permission to send to
it, then subscribe notification types to that destination. So this adapter is the transport for
SP-API notifications, and `UnwrapSellingPartnerNotification` handles the envelope they arrive in,
promoting `notificationType` and the metadata into headers so a Bitween document schema does not
have to carry Amazon's wrapper.

The SP-API *request/response* calls are ordinary HTTPS and belong in a mapper or handler, not
here. This covers the push half.

## Deduplication

At-least-once delivery is what persist-then-acknowledge buys: a crash between committing the
Xchange and acknowledging the broker redelivers **by design**. Duplicates are normal, not
exceptional, so something has to recognise them.

Each adapter supplies a key — the broker message id for RabbitMQ, the SP-API notification id for
SQS (stable across a redelivery, where the SQS `MessageId` is not), a content hash otherwise. The
host prefixes the `DataSourceId` and records it in `inbound_message`.

**The key is the primary key, and the database is the arbiter.** A duplicate is detected by the
insert *failing*, never by a lookup succeeding — "check whether it exists, then insert" is
check-then-act and races, so two concurrent deliveries of one key would both miss and both
persist. `Concurrent_deliveries_of_one_key_produce_exactly_one_Xchange` fires eight at once
precisely to prove the constraint is doing the work.

**It commits with the Xchange, in one transaction.** The sink adds the row to the same `DbContext`
the Xchange is written through, so `SubmitFilterXchange`'s existing save commits both. Splitting
them gives two failure modes and the worse one is silent: a dedupe row committing while the
Xchange fails would suppress that message for ever.

A duplicate is **accepted**, never rejected — rejecting would nack and redeliver a message that is
by definition already handled, and the queue would never drain. An event with no key is never
deduplicated, because collapsing unidentified messages would lose data.

`DataSource.DeduplicationWindowDays` (default 30, zero to disable) sets how long a key is
remembered, and `InboundMessagePruneJob` forgets them nightly. Forgetting **too early** is the
dangerous direction: a redelivery after the key is gone is processed as a fresh message. That
window is a property of the customer's broker — its message TTL, dead-letter replay, someone
re-driving a queue by hand — which is why it sits on the data source rather than in configuration.

## A trap in the PostgreSQL context

`SW.Bitween.PgSql.BitweenDbContext` does **not** call `base.OnModelCreating` — it redeclares the
model. Anything configured only in `SW.Bitween.Api`'s context is inert on the primary provider.

`DataSource` reached the model regardless, by convention, through the `BusGateway.DataSource`
navigation — which is why it worked while its intended configuration (unique index on `Name`,
explicit lengths) was silently never applied. `InboundMessage` has no such navigation and simply
did not exist until it was declared here.

Anything added to the model needs configuring in the provider contexts, not just the Api one.

## Not done yet

- **CRUD API and UI** for `DataSource`. Rows must be inserted directly for now.
- **Secret protection at rest.** `SecretProperties` names the fields; wiring it to
  `SettingsProtector` is outstanding, so treat credentials in `DataSource.Properties` as
  plaintext until that lands.
- **Re-applying `DataSource`'s intended configuration** in the provider contexts — the unique
  index on `Name` is currently missing, per the trap above.
