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

**Off by default, and single-instance only for now.** A broker connection is exclusive, so exactly
one node may hold it — and placement across nodes is not implemented yet, so every instance would
try. `DataSource.OwnedByNode` exists for that election to write into. Run this on one instance
until it lands.

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

## Deduplication is NOT enforced

Every adapter chooses its dedupe key deliberately — the broker message id for RabbitMQ, the SP-API
notification id for SQS (stable across a redelivery, where the SQS `MessageId` is not), a content
hash as the fallback — and `BusProviderEventSink` carries it onto the Xchange as a reference.

**Nothing then checks it.** A redelivered message produces a second Xchange.

At-least-once delivery is not optional here: it is what persist-then-acknowledge buys, and the
price is that duplicates are normal rather than exceptional. A crash between persisting and
acknowledging redelivers by design. So the check is required, not a refinement — the key is
carried, which is the precondition, and the enforcement is missing.

`A_notification_carries_its_notification_id_as_the_dedupe_reference` asserts the key arrives and
names this gap rather than pretending to cover it.

## Not done yet

- **Node placement and leader election** — the reason this is off by default.
- **CRUD API and UI** for `DataSource`. Rows must be inserted directly for now.
- **Secret protection at rest.** `SecretProperties` names the fields; wiring it to
  `SettingsProtector` is outstanding, so treat credentials in `DataSource.Properties` as
  plaintext until that lands.
- **Deduplication enforcement**, as above — the highest-value item on this list.
