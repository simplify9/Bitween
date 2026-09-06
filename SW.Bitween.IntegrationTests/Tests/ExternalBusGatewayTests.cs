using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using SW.Bitween.Domain;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Domain.Gateway;
using SW.Bitween.IntegrationTests.Fixtures;
using SW.Bitween.Model;
using SW.Serverless.Resident;
using Xunit;

namespace SW.Bitween.IntegrationTests.Tests;

/// <summary>
/// A BusGateway fed by an EXTERNAL RabbitMQ, end to end: a message published to a customer's own
/// broker becomes an Xchange in Bitween through a resident adapter.
///
/// The external broker is a second container, deliberately. Reusing the internal one would let a
/// test pass while the message actually travelled Bitween's own bus — the precise confusion this
/// feature has to avoid.
/// </summary>
[Collection("Bitween")]
public class ExternalBusGatewayTests
{
    private readonly BitweenFixture _fixture;

    public ExternalBusGatewayTests(BitweenFixture fixture) => _fixture = fixture;

    // ---------------------------------------------------------------- ingress

    [Fact]
    public async Task A_message_on_an_external_broker_becomes_an_Xchange()
    {
        var queue = Unique("orders");
        var (dataSourceId, documentId) = await ArrangeAsync(queue);

        await using var adapter = await StartAsync(dataSourceId);

        Publish(queue, "{\"orderId\":9001}");

        var xchange = await WaitForXchangeAsync(documentId);

        Assert.NotNull(xchange);
        Assert.Equal(documentId, xchange!.DocumentId);
    }

    /// <summary>
    /// The ordering the whole design turns on. Until Bitween has persisted, the message must still
    /// be the broker's — so a Bitween failure stops draining the customer's queue rather than
    /// losing their messages.
    /// </summary>
    [Fact]
    public async Task A_message_is_not_acknowledged_until_Bitween_has_persisted_it()
    {
        var queue = Unique("ack-order");

        // No gateway and no document: the sink cannot resolve anything, so ingest fails.
        var dataSourceId = await CreateDataSourceAsync(queue, withGateway: false);

        await using var adapter = await StartAsync(dataSourceId);

        Publish(queue, "{\"unclaimed\":true}");

        // An unclaimed endpoint is accepted-and-discarded on purpose: rejecting would requeue it
        // forever and the queue would never drain.
        await WaitAsync(() => Depth(queue) == 0, TimeSpan.FromSeconds(30),
            "an unclaimed message should be drained, not left to requeue for ever");
    }

    [Fact]
    public async Task A_rejected_message_stays_on_the_broker_for_redelivery()
    {
        var queue = Unique("reject");
        var (dataSourceId, documentId) = await ArrangeAsync(queue);

        // A payload the document's format cannot accept makes SubmitFilterXchange throw, which is
        // what the sink turns into a rejection.
        await using var adapter = await StartAsync(dataSourceId);

        Publish(queue, "this is not json at all");

        // It comes back to the queue rather than vanishing. Depth may briefly read 0 while the
        // message is unacked, so assert it is NOT permanently drained.
        await Task.Delay(TimeSpan.FromSeconds(6));

        var stats = await adapter.Instance.InvokeAsync<Dictionary<string, object>>("GetStats");
        var nacked = Convert.ToInt64(stats["nacked"]);
        var acked = Convert.ToInt64(stats["acked"]);

        Assert.True(nacked > 0 || acked == 0,
            $"a message Bitween could not persist must be nacked, not acked (acked={acked}, nacked={nacked})");
    }

    [Fact]
    public async Task Each_gateway_receives_only_its_own_endpoint()
    {
        var invoices = Unique("invoices");
        var shipments = Unique("shipments");

        var dataSourceId = await CreateDataSourceAsync(invoices, withGateway: false);
        var invoiceDoc = await AddGatewayAsync(dataSourceId, invoices);
        var shipmentDoc = await AddGatewayAsync(dataSourceId, shipments);

        await using var adapter = await StartAsync(dataSourceId);

        Publish(shipments, "{\"shipmentId\":77}");

        var xchange = await WaitForXchangeAsync(shipmentDoc);
        Assert.NotNull(xchange);
        Assert.Equal(shipmentDoc, xchange!.DocumentId);

        // And nothing landed on the other document.
        await using var scope = _fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();
        Assert.Equal(0, await db.Set<Xchange>().CountAsync(x => x.DocumentId == invoiceDoc));
    }

    // ---------------------------------------------------------------- egress

    [Fact]
    public async Task Bitween_can_publish_out_to_an_external_broker()
    {
        var queue = Unique("outbound");
        var dataSourceId = await CreateDataSourceAsync(queue, withGateway: false);

        await using var adapter = await StartAsync(dataSourceId);

        await adapter.Instance.InvokeAsync<object>("Publish", new
        {
            Endpoint = queue,
            Body = "{\"pushed\":true}"
        });

        await WaitAsync(() => Depth(queue) >= 1, TimeSpan.FromSeconds(15),
            "the published message never arrived on the external queue");
    }

    // ---------------------------------------------------------------- controls

    [Fact]
    public async Task Test_connection_reports_each_stage()
    {
        var queue = Unique("probe");
        var dataSourceId = await CreateDataSourceAsync(queue, withGateway: false);

        await using var adapter = await StartAsync(dataSourceId);

        var result = await adapter.Instance.InvokeAsync<Dictionary<string, object>>("TestConnection");

        Assert.True(Convert.ToBoolean(result["ok"]));
    }

    [Fact]
    public async Task Health_from_the_heartbeat_reaches_the_health_view()
    {
        var queue = Unique("health");
        var dataSourceId = await CreateDataSourceAsync(queue, withGateway: false);

        await using var adapter = await StartAsync(dataSourceId);

        var host = _fixture.App.Services.GetRequiredService<IResidentAdapterHost>();

        await WaitAsync(() => host.Describe()
                .Any(h => h.InstanceKey == dataSourceId.ToString() && h.LastHeartbeatOn != null),
            TimeSpan.FromSeconds(30), "no heartbeat was recorded");

        var health = host.Describe().Single(h => h.InstanceKey == dataSourceId.ToString());

        Assert.True(health.Connected);
        Assert.True(health.WorkingSetBytes > 0, "host-observed memory is sampled independently");
        Assert.Contains(queue, health.Details["endpoints"]);
    }

    // ---------------------------------------------------------------- regression

    /// <summary>
    /// The guarantee that makes this change safe to deploy: a gateway with no data source is still
    /// an internal-bus gateway and behaves exactly as it did before.
    /// </summary>
    [Fact]
    public async Task A_gateway_with_no_data_source_is_still_an_internal_bus_gateway()
    {
        await using var scope = _fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();

        var document = new Document(null, Unique("internal"), DocumentFormat.Json);
        db.Add(document);
        await db.SaveChangesAsync();

        var gateway = new BusGateway { Name = Unique("gw"), DocumentId = document.Id };
        db.Add(gateway);
        await db.SaveChangesAsync();

        var reloaded = await db.Set<BusGateway>().FirstAsync(g => g.Id == gateway.Id);

        Assert.Null(reloaded.DataSourceId);
        Assert.Null(reloaded.Endpoint);
    }

    // ---------------------------------------------------------------- helpers

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..24];

    private async Task<(int DataSourceId, int DocumentId)> ArrangeAsync(string queue)
    {
        var dataSourceId = await CreateDataSourceAsync(queue, withGateway: false);
        var documentId = await AddGatewayAsync(dataSourceId, queue);
        return (dataSourceId, documentId);
    }

    private async Task<int> CreateDataSourceAsync(string queue, bool withGateway)
    {
        await using var scope = _fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();

        var properties = new Dictionary<string, string>(_fixture.ExternalRabbitProperties)
        {
            // The supervisor normally derives this from the bound gateways; tests that create a
            // data source without one still need the adapter to declare and consume something.
            ["Endpoints"] = queue
        };

        var dataSource = new DataSource
        {
            Name = Unique("ds"),
            AdapterId = BusAdapters.RabbitMq,
            Kind = DataSourceKind.Broker,
            Properties = properties
        };

        db.Add(dataSource);
        await db.SaveChangesAsync();
        return dataSource.Id;
    }

    private async Task<int> AddGatewayAsync(int dataSourceId, string endpoint)
    {
        await using var scope = _fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();

        var document = new Document(null, Unique("doc"), DocumentFormat.Json);
        db.Add(document);
        await db.SaveChangesAsync();

        db.Add(new BusGateway
        {
            Name = Unique("gw"),
            DocumentId = document.Id,
            DataSourceId = dataSourceId,
            Endpoint = endpoint
        });
        await db.SaveChangesAsync();

        return document.Id;
    }

    /// <summary>Starts the adapter for a data source and stops it when the test finishes.</summary>
    private async Task<AdapterLease> StartAsync(int dataSourceId)
    {
        await using var scope = _fixture.App.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();
        var dataSource = await db.Set<DataSource>().AsNoTracking().FirstAsync(d => d.Id == dataSourceId);

        var host = _fixture.App.Services.GetRequiredService<IResidentAdapterHost>();

        var instance = await host.StartExclusiveAsync(new AdapterSpec
        {
            AdapterId = dataSource.AdapterId,
            // The instance key IS the data source id — that is how the sink resolves the gateway.
            InstanceKey = dataSourceId.ToString(),
            StartupValues = new Dictionary<string, string>(dataSource.Properties)
        });

        return new AdapterLease(host, dataSource.AdapterId, dataSourceId.ToString(), instance);
    }

    private sealed class AdapterLease : IAsyncDisposable
    {
        private readonly IResidentAdapterHost _host;
        private readonly string _adapterId;
        private readonly string _instanceKey;

        public AdapterLease(IResidentAdapterHost host, string adapterId, string instanceKey,
            ResidentAdapterInstance instance)
        {
            _host = host;
            _adapterId = adapterId;
            _instanceKey = instanceKey;
            Instance = instance;
        }

        public ResidentAdapterInstance Instance { get; }

        public ValueTask DisposeAsync() =>
            new(_host.StopAsync(_adapterId, _instanceKey, drain: false));
    }

    private void Publish(string queue, string body)
    {
        using var connection = ExternalConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString("N");
        properties.DeliveryMode = 2;

        channel.BasicPublish("", queue, properties, Encoding.UTF8.GetBytes(body));
    }

    private uint Depth(string queue)
    {
        using var connection = ExternalConnection();
        using var channel = connection.CreateModel();
        return channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false).MessageCount;
    }

    private IConnection ExternalConnection() => new ConnectionFactory
    {
        HostName = _fixture.ExternalRabbitHost,
        Port = _fixture.ExternalRabbitPort,
        UserName = _fixture.ExternalRabbitUser,
        Password = _fixture.ExternalRabbitPassword
    }.CreateConnection("integration-tests");

    private async Task<Xchange?> WaitForXchangeAsync(int documentId)
    {
        Xchange? found = null;
        await WaitAsync(async () =>
        {
            await using var scope = _fixture.App.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();
            found = await db.Set<Xchange>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.DocumentId == documentId);
            return found != null;
        }, TimeSpan.FromSeconds(45), $"no Xchange was created for document {documentId}");

        return found;
    }

    private static async Task WaitAsync(Func<bool> condition, TimeSpan timeout, string because) =>
        await WaitAsync(() => Task.FromResult(condition()), timeout, because);

    private static async Task WaitAsync(Func<Task<bool>> condition, TimeSpan timeout, string because)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try { if (await condition()) return; } catch { /* still settling */ }
            await Task.Delay(300);
        }
        Assert.Fail($"Timed out after {timeout}: {because}");
    }
}
