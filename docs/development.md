# Development

## Prerequisites

- .NET 10 SDK
- Node 22 and Yarn 1
- Docker, for local dependencies and the integration tests
- `dotnet-ef` 9, for migrations

## Running locally

Follow the [quick start](../README.md#quick-start). Keep local configuration in environment variables or in `SW.Bitween.Web/appsettings.Development.json`, which git ignores.

The launch profile serves `https://localhost:5000` and `http://localhost:5003`, with Swagger UI at `/swagger`.

To work on the UI, run `yarn build` in `SW.Bitween.Web/ClientApp` and refresh the browser. `dotnet publish` builds the UI itself unless you pass `-p:SkipClientBuild=true`.

## Tests

| Suite | Command | Needs |
|---|---|---|
| Unit tests, MSTest | `dotnet test SW.Bitween.UnitTests` | Nothing. Uses in-memory SQLite and fakes. |
| Integration tests, xUnit | `dotnet test SW.Bitween.IntegrationTests` | A running Docker daemon |
| UI unit tests, Vitest | `yarn test` in `ClientApp` | Nothing |
| UI end-to-end, Playwright | `yarn test:e2e` in `ClientApp` | The API at `https://localhost:7155` with a real database |

- The integration tests start PostgreSQL, RabbitMQ and MailHog with Testcontainers. They run migrations, use local-disk storage and the real serverless runner, and replace Quartz with a recording fake. The build publishes the sample adapters and the tests install them into local storage, so custom adapters run for real. Tests run one at a time.
- `MigrationDriftTests` fails when the model changed without a migration for all three providers.
- The Rebex POP3 tests are inconclusive unless `Bitween__RebexLicenseKey` is set.
- The database adapter tests start PostgreSQL 16, MySQL 8.4, SQL Server 2022 and Oracle Free 23 containers, and the broker adapter tests run against containers too. The first run downloads large images.
- The integration tests use their own local storage bucket, `bitween-integration-tests`, so they leave a local instance's adapters alone.
- UI unit tests and Playwright specs are type-checked through `tsconfig.test.json`.
- The Playwright setup signs in as the seeded administrator, falling back to break-glass credentials `1:1`, and cleans up its own test data before each run. It expects a launch profile on port 7155 that is not committed.

## Trying data sources locally

`tools/dev-database.md` explains how to publish the broker and database adapter packages to local storage and how to seed a database for each engine with the `tools/dev-warehouse*.sql` scripts. Set `Bitween__BusProvidersEnabled=true` on the local instance.

## Adding a database migration

Each provider project has a design-time factory, so no startup project or running database is needed. Add the migration to all three providers with the same name.

```bash
dotnet tool install -g dotnet-ef --version 9.0.19
(cd SW.Bitween.PgSql && dotnet ef migrations add <Name>)
(cd SW.Bitween.MySql && dotnet ef migrations add <Name>)
(cd SW.Bitween.MsSql && dotnet ef migrations add <Name>)
dotnet test SW.Bitween.UnitTests --filter MigrationDriftTests
```

The PostgreSQL context declares its own model rather than inheriting the shared one. Make mapping changes in both `SW.Bitween.Api/Data/BitweenDbContext.cs` and `SW.Bitween.PgSql/BitweenDbContext.cs`.

`migratedb.sh` refers to project names from an older version and does not work.

## Adding an API endpoint

Add a class under `SW.Bitween.Api/Resources/{Area}` that implements a CqApi handler interface. The folder name, lower-cased, becomes the path. These rules are inferred from the existing handlers and the routes the UI calls.

| Handler | Route |
|---|---|
| `ISearchyHandler` or keyless `IQueryHandler` | `GET /api/{area}` |
| `IGetHandler<TKey, …>` | `GET /api/{area}/{key}` |
| Keyless `ICommandHandler<TRequest, …>` | `POST /api/{area}` |
| Keyed `ICommandHandler<TKey, TRequest, …>` named `Update` | `POST /api/{area}/{key}` |
| Keyed handler with any other class name | Adds the class name, as in `POST /api/apigateways/{id}/addpartner` |
| `IDeleteHandler<TKey, …>` | `DELETE /api/{area}/{key}` |
| `[HandlerName("name")]` | Adds `/name` |

Every endpoint needs a signed-in caller unless the class has `[Unprotect]`. Check the permission first thing in the handler.

```csharp
await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Subscriptions.Edit);
```

Always save with `SaveChangesAsync`. The synchronous `SaveChanges` throws, because it would skip the audit trail. After changing cached configuration, call `IInfolinkCache.BroadcastRevoke()` so every instance reloads.

## Adding a native adapter

1. Create a folder in `SW.Bitween.NativeAdapters` with an input class. Annotate its properties with `[Required]`, `[Description]`, `[DefaultValue]` and, for secrets, `[Secure]`.
2. Create the adapter class. Implement `INativeInfolinkHandler`, `INativeInfolinkReceiver`, `INativeInfolinkValidator` or `INativeInfolinkMapper`. Its `Name` must start with `Native`, `StartupValuesType` returns the input class, and `InitializeStartupValues` reads the properties.
3. Register it twice in `ServiceCollectionExtensions.AddNativeAdapters`: once as its kind's interface and once as `INativeAdapter`.
4. Implement `IRequiresRebexLicense` if it needs Rebex. Implement `IReceivesMappingContext` if it is a mapper that should receive partner and global values as context.

Adapters are found by name, ignoring case, and the name is the id stored on subscriptions. Renaming an adapter breaks every subscription that uses it.

## Adding a permission

1. Add the constant and its catalogue entry in `SW.Bitween.Sdk/Model/Permissions.cs`. `PermissionCatalogTests` checks that the two agree.
2. Guard the handlers with `EnsurePermission`.
3. Gate the route in `ClientApp/src/router.tsx` and the navigation entry in `ClientApp/src/nav.ts`.

Administrators get new permissions automatically. Members and Viewers get them when the area belongs to the Operate, Subscriptions or Configuration group.

## Adding a setting

1. Add the property to `BitweenOptions` or `ThemeOptions`.
2. Add a definition to `SettingsCatalog`. Make it editable only if every consumer reads the options object each time it uses the value. Anything read once at startup must be read-only.
3. Add an `OnChange` action when assigning the value is not enough, as the retry job cron does.

## Adding a scheduled job

Model it on `ReceiveAttemptCleanupJob`: mark the class with `[ScheduleConfig(AllowConcurrentExecution = false, MisfireInstructions = Skip)]`, and register its schedule in `SchedulerSeedService`.

## Conventions

- Code comments explain decisions in detail. Read them before changing behaviour.
- `.editorconfig` prefers primary constructors, file-scoped namespaces and pattern matching.
- The admin UI's permission catalogue is served by the API, so the backend is the single source of truth.
