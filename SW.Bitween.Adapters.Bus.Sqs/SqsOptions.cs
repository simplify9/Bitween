namespace SW.Bitween.Adapters.Bus.Sqs;

public class SqsOptions
{
    /// <summary>
    /// False creates the client but starts no pollers. This is what a connection test runs as:
    /// without it, testing a data source would start receiving from the customer's queue.
    /// </summary>
    public bool Consume { get; set; } = true;

    public string Region { get; set; } = "eu-west-1";

    /// <summary>
    /// Leave both blank to use the ambient credential chain — instance profile, IRSA, or the
    /// environment. That is the right answer on AWS; explicit keys are for everything else.
    /// </summary>
    public string AccessKeyId { get; set; }
    public string SecretAccessKey { get; set; }

    /// <summary>Override for LocalStack or ElasticMQ in development.</summary>
    public string ServiceUrl { get; set; }

    /// <summary>Comma-separated queue URLs, supplied by the gateways bound to this data source.</summary>
    public string Endpoints { get; set; }

    /// <summary>Long polling. 20 is the maximum and the only sensible value — 0 burns money and API calls.</summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>Max 10 per receive; that is an SQS limit, not a choice.</summary>
    public int MaxMessagesPerReceive { get; set; } = 10;

    /// <summary>
    /// How long a received message stays invisible to other consumers. It must exceed the time
    /// Bitween takes to persist, or the message is redelivered while still being handled.
    /// </summary>
    public int VisibilityTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// SP-API wraps its notifications in an envelope. On, the adapter forwards only the payload
    /// and promotes notificationType and the SP-API metadata into headers.
    /// </summary>
    public bool UnwrapSellingPartnerNotification { get; set; }
}
