namespace SW.Bitween.Adapters.Bus.RabbitMq;

/// <summary>
/// Every one of these arrives as a DataSource property, bound by name. Nothing here is opinionated
/// about how the broker should be laid out: the queue may already exist and be owned by someone
/// else, or Bitween may declare it — <see cref="DeclareMode"/> decides which.
///
/// The attributes are not documentation: Bitween reads them out of this assembly to build the form
/// an operator fills in, so a field added here appears there without anyone editing the UI.
/// </summary>
[AdapterSettings(
    Kind = "Broker",
    Label = "RabbitMQ",
    Description = "An AMQP broker the customer runs, separate from Bitween's own bus.")]
public class RabbitOptions
{
    [AdapterSetting(Required = true,
        Hint = "Host name only — no amqp:// or amqps:// prefix, and no credentials.")]
    public string Host { get; set; } = "localhost";

    [AdapterSetting(Default = "5672",
        Hint = "5672 plain, 5671 for TLS. A hosted broker is almost always 5671.")]
    public int Port { get; set; } = 5672;

    [AdapterSetting(Required = true)]
    public string UserName { get; set; } = "guest";

    [AdapterSetting(Secret = true, Required = true)]
    public string Password { get; set; }

    [AdapterSetting(Default = "/",
        Hint = "On a shared hosted broker this is usually the same as the user name.")]
    public string VirtualHost { get; set; } = "/";

    [AdapterSetting(Default = "false", AllowedValues = new[] {"true", "false"},
        Hint = "Set for anything that is not localhost — AMQP authenticates in the clear otherwise. "
               + "Pair it with port 5671.")]
    public bool UseSsl { get; set; }

    /// <summary>Comma-separated queue names, supplied by the gateways bound to this data source.</summary>
    [AdapterSetting(Hidden = true)]
    public string Endpoints { get; set; }

    /// <summary>
    /// none   — assume everything exists; never touch the topology.
    /// assert — verify it exists and fail loudly if not (passive declare).
    /// create — declare queues, and an exchange and binding when Exchange is set.
    /// </summary>
    [AdapterSetting(Default = "assert", AllowedValues = new[] {"none", "assert", "create"},
        Hint = "none — assume everything exists. assert — check and fail loudly if not. "
               + "create — declare the queues.")]
    public string DeclareMode { get; set; } = "assert";

    /// <summary>Optional. When set, each endpoint queue is bound to it.</summary>
    [AdapterSetting(Hint = "Optional. When set, each endpoint queue is bound to it.")]
    public string Exchange { get; set; }

    [AdapterSetting(Default = "topic", AllowedValues = new[] {"direct", "fanout", "topic", "headers"},
        Hint = "Only used when Exchange is set.")]
    public string ExchangeType { get; set; } = "topic";

    [AdapterSetting(Hint = "Binding key for the exchange. Only used when Exchange is set.")]
    public string RoutingKey { get; set; }

    /// <summary>
    /// False connects and declares but does not consume. This is what a connection test runs as:
    /// without it, testing a data source would start pulling messages off the customer's queue.
    /// </summary>
    [AdapterSetting(Hidden = true)]
    public bool Consume { get; set; } = true;

    /// <summary>Broker-side backpressure; pairs with the host's credit window.</summary>
    [AdapterSetting(Default = "16",
        Hint = "How many messages the broker lets Bitween hold unacknowledged at once.")]
    public ushort Prefetch { get; set; } = 16;

    [AdapterSetting(Default = "true", AllowedValues = new[] {"true", "false"},
        Hint = "Declared queues survive a broker restart. Only read when DeclareMode is create.")]
    public bool Durable { get; set; } = true;

    /// <summary>Passed straight through as x-queue-type: classic, quorum or stream.</summary>
    [AdapterSetting(AllowedValues = new[] {"classic", "quorum", "stream"},
        Hint = "Passed through as x-queue-type when Bitween declares the queue. Leave empty to let "
               + "the broker choose. It cannot be changed on a queue that already exists.")]
    public string QueueType { get; set; }
}
