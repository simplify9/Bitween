namespace SW.Bitween.Adapters.Bus.RabbitMq;

/// <summary>
/// Every one of these arrives as a DataSource property, bound by name. Nothing here is opinionated
/// about how the broker should be laid out: the queue may already exist and be owned by someone
/// else, or Bitween may declare it — <see cref="DeclareMode"/> decides which.
/// </summary>
public class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; }
    public string VirtualHost { get; set; } = "/";
    public bool UseSsl { get; set; }

    /// <summary>Comma-separated queue names, supplied by the gateways bound to this data source.</summary>
    public string Endpoints { get; set; }

    /// <summary>
    /// none   — assume everything exists; never touch the topology.
    /// assert — verify it exists and fail loudly if not (passive declare).
    /// create — declare queues, and an exchange and binding when Exchange is set.
    /// </summary>
    public string DeclareMode { get; set; } = "assert";

    /// <summary>Optional. When set, each endpoint queue is bound to it.</summary>
    public string Exchange { get; set; }
    public string ExchangeType { get; set; } = "topic";
    public string RoutingKey { get; set; }

    /// <summary>
    /// False connects and declares but does not consume. This is what a connection test runs as:
    /// without it, testing a data source would start pulling messages off the customer's queue.
    /// </summary>
    public bool Consume { get; set; } = true;

    /// <summary>Broker-side backpressure; pairs with the host's credit window.</summary>
    public ushort Prefetch { get; set; } = 16;

    public bool Durable { get; set; } = true;

    /// <summary>Passed straight through as x-queue-type: classic, quorum or stream.</summary>
    public string QueueType { get; set; }
}
