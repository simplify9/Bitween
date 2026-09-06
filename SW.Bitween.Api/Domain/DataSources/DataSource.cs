using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;

namespace SW.Bitween.Domain.DataSources;

/// <summary>
/// How to reach an external system, separate from what Bitween does with it.
///
/// A <see cref="Gateway.BusGateway"/> with no <c>DataSourceId</c> still means the INTERNAL bus, so
/// every gateway that exists today keeps working untouched. Setting one moves that gateway onto an
/// external broker, served by a resident serverless adapter.
///
/// The split matters: this holds the connection — endpoint, credentials, health — and the gateway
/// holds the meaning — which Document, which routes, which filters. One data source can serve many
/// gateways, exactly as one RabbitMQ connection serves many queues.
/// </summary>
public class DataSource : BaseEntity, IAudited
{
    public string Name { get; set; }

    /// <summary>
    /// The adapter that knows this protocol, e.g. <c>bitween.bus.rabbitmq</c> or
    /// <c>bitween.bus.sqs</c>. Resolved and installed from cloud storage like any other adapter.
    /// </summary>
    public string AdapterId { get; set; }

    /// <summary>Free text for the UI; the adapter is the authority on what it actually speaks.</summary>
    public DataSourceKind Kind { get; set; } = DataSourceKind.Broker;

    /// <summary>
    /// Connection settings handed to the adapter as startup values. Deliberately untyped: a
    /// provider must not be constrained to the subset of a broker's model that Bitween happens to
    /// have modelled. Secrets are protected at rest — see <see cref="SecretProperties"/>.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Names within <see cref="Properties"/> whose values are encrypted at rest and never returned
    /// by the API in clear.
    /// </summary>
    public List<string> SecretProperties { get; set; } = new();

    /// <summary>Stops the adapter without deleting the configuration, mirroring BusGateway.Inactive.</summary>
    public bool Inactive { get; set; }

    /// <summary>
    /// How long a message's dedupe key is remembered. It has to exceed the widest redelivery
    /// window this broker can actually produce — its message TTL, a dead-letter replay, someone
    /// re-driving a queue by hand — because a key forgotten too early lets a redelivery through
    /// as a fresh message. That number is a property of the customer's broker, not of Bitween,
    /// which is why it lives here rather than in configuration.
    ///
    /// Zero turns deduplication off for this data source.
    /// </summary>
    public int DeduplicationWindowDays { get; set; } = 30;

    // ---------------------------------------------------------------- health

    /// <summary>Last state the adapter reported on its heartbeat: Connected, Idle, Disconnected...</summary>
    public string LastKnownState { get; set; }

    public DateTime? LastHeartbeatOn { get; set; }
    public string LastException { get; set; }
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Which node currently owns this connection. A broker connection is exclusive, so exactly one
    /// node may hold it; this is what the placement layer writes.
    /// </summary>
    public string OwnedByNode { get; set; }

    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }
}

/// <summary>
/// Broker today. The others are declared now because the adapter contract is identical for them —
/// a relational or object-store source differs in whether it pushes or is polled, not in how it is
/// configured, supervised or observed.
/// </summary>
public enum DataSourceKind
{
    Broker = 0,
    Relational = 1,
    Document = 2,
    ObjectStore = 3,
    Http = 4
}
