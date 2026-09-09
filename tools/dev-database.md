# Dev database: a working set to develop against

The dev database ships with demo rows that **cannot be saved**: every subscription is a `Receiving`
type with no receiver and no schedule, which the update validator refuses. So opening any of them
in the UI and pressing Save fails, and nothing in the seed exercises a database data source at all.

This is how to get a dev environment whose records are valid and whose data sources actually
connect to something.

## 1. A database to integrate with

`dev-warehouse.sql` builds the shape an ERP integration actually meets — customers, orders,
order lines, an outbox that an integration drains, a set-returning function, a stored procedure and
a sequence — with 60 orders and 30 unsent shipment notifications.

```bash
docker run -d --name bw-sample-db \
  -e POSTGRES_USER=warehouse -e POSTGRES_PASSWORD=warehouse -e POSTGRES_DB=warehouse \
  -p 55440:5432 postgres:16-alpine

docker cp tools/dev-warehouse.sql bw-sample-db:/seed.sql
docker exec bw-sample-db psql -U warehouse -d warehouse -f /seed.sql
```

## 2. Publish the database adapter

The provider only appears in the data source form once its package is in cloud storage. Publish it
through `ICloudFilesService` — **not** by copying files into the local store by hand. The object's
metadata is what `AdapterInstaller` reads, and a hand-written sidecar reads back inconsistently:
the adapter appeared to work, then failed later with "missing 'EntryAssembly'" once a cache expired.

With `StorageProvider: Local` the bucket resolves from configuration, so an unset bucket name puts
the object under `.../SW.CloudFiles.LocalTests/default/adapters/`.

## 3. Repair the seeded subscriptions

Give each `Receiving` subscription a receiver and a schedule — through the API, so validation runs:

```
receiverId          native.httpreceiver
receiverProperties  Url = https://api.example/orders
schedules           one Hourly, 30 minutes
```

An hourly schedule with `hours: 1` is rejected as "Invalid hourly schedule"; the offset has to be
inside the hour, so use minutes.

## 4. Two subscriptions that use the database

Once a `Warehouse PostgreSQL` data source exists with statements on it:

| Subscription | Slot | Statement | Operation |
|---|---|---|---|
| Warehouse Outbox Drain | receiver | `drainOutbox` | query |
| Warehouse Order Intake | handler | `insertOrder` | execute |

The data source needs its receive settings for the drain to work: `ReceiveMode = marker`,
`KeyColumn = id`, and a `MarkProcessedStatement` that sets `processed`. Marker mode has no cursor,
so the mark is the only thing stopping the same rows being read on every poll — which is why the
adapter refuses to start in that mode without one.

## Proving it works

`POST /subscriptions/8/receivenow` drains a batch. With 30 unsent rows and a batch size of 25, two
polls produce 30 exchanges and leave the outbox empty, every row marked processed.
