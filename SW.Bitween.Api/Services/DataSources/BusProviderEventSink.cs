using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SW.Bitween.Domain.Gateway;
using SW.EfCoreExtensions;
using SW.PrimitiveTypes;
using SW.Serverless.Resident;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SW.Bitween.Services.DataSources;

/// <summary>
/// Where a message from an external broker enters Bitween.
///
/// The adapter owns the connection and hands the payload over; everything after that is the path
/// an internally-bussed message already takes — <see cref="XchangeService.SubmitFilterXchange"/>,
/// which persists the Xchange, writes the payload to cloud storage and lets the filter decide
/// which subscriptions run. Ingress from a broker and ingress from the API are the same thing
/// past this point, deliberately.
///
/// The adapter does NOT acknowledge its broker until this returns Accepted. That ordering is the
/// whole contract: persist, then ack. A crash in between means redelivery, which is why every
/// event carries a dedupe key.
/// </summary>
public class BusProviderEventSink : IAdapterEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusProviderEventSink> _logger;

    public BusProviderEventSink(IServiceProvider serviceProvider, ILogger<BusProviderEventSink> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<EventOutcome> OnEventAsync(InboundEvent inboundEvent, CancellationToken cancellationToken)
    {
        // A resident adapter is a singleton and outlives any request, so ingest gets its own scope
        // per message rather than borrowing one.
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();
        var xchangeService = scope.ServiceProvider.GetRequiredService<XchangeService>();

        if (!int.TryParse(inboundEvent.InstanceKey, out var dataSourceId))
            return EventOutcome.Rejected($"'{inboundEvent.InstanceKey}' is not a data source id.");

        // Which gateway on this data source owns this endpoint. Endpoint is what the adapter was
        // told to consume — a queue, a topic, an SQS URL.
        var gateway = await dbContext.Set<BusGateway>()
            .Where(g => g.DataSourceId == dataSourceId && !g.Inactive)
            .Where(g => g.Endpoint == inboundEvent.Endpoint || g.Endpoint == null)
            .OrderByDescending(g => g.Endpoint)   // an exact endpoint match beats the catch-all
            .FirstOrDefaultAsync(cancellationToken);

        if (gateway == null)
        {
            // Not an error: the adapter is consuming something no gateway claims. Rejecting would
            // requeue it forever, so accept and drop with a warning instead.
            _logger.LogWarning(
                "No active bus gateway on data source {DataSourceId} claims endpoint '{Endpoint}'. Discarding.",
                dataSourceId, inboundEvent.Endpoint);
            return EventOutcome.Ok("unclaimed");
        }

        try
        {
            var payload = Encoding.UTF8.GetString(inboundEvent.Payload ?? Array.Empty<byte>());
            var file = new XchangeFile(payload);

            // Same entry point the internal bus uses. Filtering, mapping, handling, auto-retry and
            // the audit trail all follow from here unchanged.
            await xchangeService.SubmitFilterXchange(
                gateway.DocumentId,
                file,
                references: string.IsNullOrEmpty(inboundEvent.DedupeKey)
                    ? null
                    : new[] { inboundEvent.DedupeKey },
                correlationId: inboundEvent.Traceparent);

            return EventOutcome.Ok(inboundEvent.DedupeKey);
        }
        catch (Exception ex)
        {
            // Rejecting is the right answer: the adapter nacks, the broker redelivers, and nothing
            // is silently lost because Bitween happened to be unhealthy for a moment.
            _logger.LogError(ex, "Failed to ingest a message from data source {DataSourceId} endpoint {Endpoint}.",
                dataSourceId, inboundEvent.Endpoint);
            return EventOutcome.Rejected(ex.Message);
        }
    }
}
