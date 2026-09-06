using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SW.Serverless.Sdk.Resident;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Bus.RabbitMq;

/// <summary>
/// Bitween's external RabbitMQ bus provider — a broker that is NOT the internal one, owned by
/// someone else, whose queues Bitween consumes and publishes to.
///
/// The ack ordering is the contract:
///
///     delivery -> PublishAsync to the host -> host persists the Xchange -> ack returns
///              -> ONLY THEN BasicAck
///
/// A host rejection becomes BasicNack(requeue: true), so a Bitween outage does not lose the
/// customer's messages — it just stops draining their queue, which is the correct failure.
/// </summary>
public class RabbitBusHandler : IResidentAdapter
{
    private readonly RabbitOptions _options;
    private readonly ILogger<RabbitBusHandler> _logger;

    private IAdapterContext _context;
    private IConnection _connection;
    private IModel _consumeChannel;
    private IModel _publishChannel;
    private CancellationTokenSource _stopping;

    private readonly List<string> _endpoints = new();
    private readonly Dictionary<string, string> _consumerTags = new();

    private long _received, _acked, _nacked, _failed, _published;
    private DateTimeOffset? _lastMessageOn;
    private string _lastError;
    private volatile string _state = "Starting";

    public RabbitBusHandler(IOptions<RabbitOptions> options, ILogger<RabbitBusHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // ---------------------------------------------------------------- lifecycle

    public Task StartAsync(IAdapterContext context, CancellationToken cancellationToken)
    {
        _context = context;
        _stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _endpoints.AddRange((_options.Endpoints ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password ?? "",
            VirtualHost = _options.VirtualHost,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(15),

            // The supervisor owns restart policy — backoff, crash-loop quarantine, health
            // write-back. A second, hidden recovery loop in here would fight it.
            AutomaticRecoveryEnabled = false
        };
        if (_options.UseSsl) factory.Ssl = new SslOption { Enabled = true, ServerName = _options.Host };

        _connection = factory.CreateConnection($"bitween-{context.InstanceKey}");
        _connection.ConnectionShutdown += (_, e) =>
        {
            _state = "Disconnected";
            _lastError = $"{e.ReplyCode} {e.ReplyText}";
            _logger.LogWarning("Connection to {Host} closed: {Reason}", _options.Host, e.ReplyText);
        };

        _publishChannel = _connection.CreateModel();
        _consumeChannel = _connection.CreateModel();
        _consumeChannel.BasicQos(0, _options.Prefetch, global: false);

        DeclareTopology(_consumeChannel);

        foreach (var endpoint in _endpoints)
        {
            var consumer = new EventingBasicConsumer(_consumeChannel);
            consumer.Received += (_, delivery) => _ = Task.Run(() => HandleAsync(endpoint, delivery));

            // autoAck: false is what makes persist-then-ack possible at all.
            _consumerTags[endpoint] = _consumeChannel.BasicConsume(endpoint, autoAck: false, consumer);
        }

        _state = _endpoints.Count == 0 ? "Idle" : "Connected";
        _logger.LogInformation("Connected to {Host}:{Port}{VHost}, consuming {Count} endpoint(s) with prefetch {Prefetch}.",
            _options.Host, _options.Port, _options.VirtualHost, _endpoints.Count, _options.Prefetch);

        return Task.CompletedTask;
    }

    private void DeclareTopology(IModel channel)
    {
        var mode = (_options.DeclareMode ?? "assert").ToLowerInvariant();
        if (mode == "none") return;

        if (mode == "create" && !string.IsNullOrWhiteSpace(_options.Exchange))
            channel.ExchangeDeclare(_options.Exchange, _options.ExchangeType ?? "topic",
                durable: _options.Durable, autoDelete: false);

        foreach (var endpoint in _endpoints)
        {
            if (mode == "assert")
            {
                // Throws if it does not exist, which is what we want: better to fail at start than
                // to silently create a queue on someone else's broker.
                channel.QueueDeclarePassive(endpoint);
                continue;
            }

            var arguments = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(_options.QueueType)) arguments["x-queue-type"] = _options.QueueType;

            channel.QueueDeclare(endpoint, durable: _options.Durable, exclusive: false,
                autoDelete: false, arguments: arguments.Count == 0 ? null : arguments);

            if (!string.IsNullOrWhiteSpace(_options.Exchange))
                channel.QueueBind(endpoint, _options.Exchange, _options.RoutingKey ?? endpoint);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _state = "Draining";
        _stopping?.Cancel();

        foreach (var tag in _consumerTags.Values)
            try { _consumeChannel?.BasicCancel(tag); } catch { }

        try { _consumeChannel?.Close(); _publishChannel?.Close(); } catch { }
        try { _connection?.Close(TimeSpan.FromSeconds(3)); } catch { }
        _connection?.Dispose();

        _state = "Stopped";
        return Task.CompletedTask;
    }

    public Task<AdapterStatus> GetStatusAsync()
    {
        var status = new AdapterStatus
        {
            Connected = _connection?.IsOpen == true,
            State = _connection?.IsOpen != true ? "Disconnected"
                  : _received == 0 ? "Idle" : _state,
            LastMessageOn = _lastMessageOn,
            LastError = _lastError,
            InFlight = Math.Max(0, _received - _acked - _nacked - _failed)
        };

        status.Details["host"] = $"{_options.Host}:{_options.Port}{_options.VirtualHost}";
        status.Details["endpoints"] = string.Join(",", _endpoints);
        status.Details["prefetch"] = _options.Prefetch.ToString();
        status.Details["received"] = _received.ToString();
        status.Details["acked"] = _acked.ToString();
        status.Details["nacked"] = _nacked.ToString();
        status.Details["failed"] = _failed.ToString();
        status.Details["published"] = _published.ToString();

        foreach (var endpoint in _endpoints)
            status.Details[$"depth:{endpoint}"] = Depth(endpoint)?.ToString() ?? "?";

        return Task.FromResult(status);
    }

    private uint? Depth(string queue)
    {
        try
        {
            using var probe = _connection.CreateModel();
            return probe.QueueDeclarePassive(queue).MessageCount;
        }
        catch { return null; }
    }

    // ---------------------------------------------------------------- ingress

    private async Task HandleAsync(string endpoint, BasicDeliverEventArgs delivery)
    {
        Interlocked.Increment(ref _received);

        try
        {
            var headers = new Dictionary<string, string>
            {
                ["rabbit.exchange"] = delivery.Exchange,
                ["rabbit.routingKey"] = delivery.RoutingKey,
                ["rabbit.redelivered"] = delivery.Redelivered.ToString()
            };
            if (delivery.BasicProperties?.MessageId is { Length: > 0 } messageId)
                headers["rabbit.messageId"] = messageId;

            var result = await _context.PublishAsync(
                delivery.Body,
                // The broker's own message id when it has one, otherwise a content hash. NOT the
                // delivery tag: tags are per channel and restart at 1 on every reconnect.
                dedupeKey: delivery.BasicProperties?.MessageId is { Length: > 0 } id
                    ? $"rabbit:{_options.Host}:{endpoint}:{id}"
                    : $"rabbit:{_options.Host}:{endpoint}:{Convert.ToHexString(SHA256.HashData(delivery.Body.Span))[..32]}",
                endpoint: endpoint,
                headers: headers,
                contentType: delivery.BasicProperties?.ContentType ?? "application/json",
                cancellationToken: _stopping.Token);

            if (result.Accepted)
            {
                _consumeChannel.BasicAck(delivery.DeliveryTag, multiple: false);
                Interlocked.Increment(ref _acked);
                _lastMessageOn = DateTimeOffset.UtcNow;
                _context.Metric("bitween.bus.rabbitmq.acked", 1);
            }
            else
            {
                _consumeChannel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
                Interlocked.Increment(ref _nacked);
                _lastError = result.Error;
                _logger.LogWarning("Bitween rejected a message from {Endpoint}: {Error}. Requeued.",
                    endpoint, result.Error);
            }
        }
        catch (OperationCanceledException)
        {
            try { _consumeChannel.BasicNack(delivery.DeliveryTag, false, requeue: true); } catch { }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failed);
            _lastError = ex.Message;
            _logger.LogError(ex, "Failed to hand a delivery from {Endpoint} to Bitween.", endpoint);
            try { _consumeChannel.BasicNack(delivery.DeliveryTag, false, requeue: true); } catch { }
        }
    }

    // ---------------------------------------------------------------- commands

    /// <summary>
    /// Egress. Bitween does not have this on the internal gateway yet; an external provider gets
    /// it for free because the adapter owns the connection either way.
    /// </summary>
    public Task<object> Publish(PublishRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Endpoint) && string.IsNullOrWhiteSpace(request?.Exchange))
            throw new ArgumentException("Either Endpoint or Exchange is required.");

        var properties = _publishChannel.CreateBasicProperties();
        properties.ContentType = request.ContentType ?? "application/json";
        properties.MessageId = request.MessageId ?? Guid.NewGuid().ToString("N");
        properties.DeliveryMode = (byte)(_options.Durable ? 2 : 1);

        var body = System.Text.Encoding.UTF8.GetBytes(request.Body ?? "");

        lock (_publishChannel)
            _publishChannel.BasicPublish(
                exchange: request.Exchange ?? "",
                routingKey: request.Exchange == null ? request.Endpoint : request.RoutingKey ?? "",
                mandatory: false,
                basicProperties: properties,
                body: body);

        Interlocked.Increment(ref _published);
        return Task.FromResult<object>(new { messageId = properties.MessageId, bytes = body.Length });
    }

    /// <summary>The control the UI needs before a data source is saved. Staged, so a failure names the step.</summary>
    public Task<object> TestConnection()
    {
        var steps = new List<object>();
        IConnection probe = null;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host, Port = _options.Port,
                UserName = _options.UserName, Password = _options.Password ?? "",
                VirtualHost = _options.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };
            if (_options.UseSsl) factory.Ssl = new SslOption { Enabled = true, ServerName = _options.Host };

            probe = factory.CreateConnection("bitween-probe");
            steps.Add(new { step = "connect", ok = true, detail = probe.Endpoint.ToString() });

            using var channel = probe.CreateModel();
            steps.Add(new { step = "authenticate", ok = true, detail = _options.VirtualHost });

            foreach (var endpoint in _endpoints)
            {
                try
                {
                    var declared = channel.QueueDeclarePassive(endpoint);
                    steps.Add(new { step = $"queue:{endpoint}", ok = true, detail = $"{declared.MessageCount} message(s)" });
                }
                catch (Exception ex)
                {
                    steps.Add(new { step = $"queue:{endpoint}", ok = false, detail = ex.Message });
                    return Task.FromResult<object>(new { ok = false, steps });
                }
            }

            return Task.FromResult<object>(new { ok = true, steps });
        }
        catch (Exception ex)
        {
            steps.Add(new { step = "failed", ok = false, detail = ex.Message });
            return Task.FromResult<object>(new { ok = false, steps });
        }
        finally
        {
            try { probe?.Close(); probe?.Dispose(); } catch { }
        }
    }

    /// <summary>What is actually on the broker, for the "pick a queue" step in the UI.</summary>
    public Task<object> Discover() => Task.FromResult<object>(new
    {
        host = $"{_options.Host}:{_options.Port}",
        virtualHost = _options.VirtualHost,
        endpoints = _endpoints.Select(e => new { name = e, messages = Depth(e), consuming = _consumerTags.ContainsKey(e) }),
        note = "AMQP alone can only report on queues we were told about. " +
               "Listing everything on the broker needs the management plugin."
    });

    public Task<object> GetStats() => Task.FromResult<object>(new
    {
        received = _received, acked = _acked, nacked = _nacked, failed = _failed, published = _published,
        endpoints = _endpoints, prefetch = _options.Prefetch
    });

    public class PublishRequest
    {
        public string Endpoint { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string MessageId { get; set; }
        public string ContentType { get; set; }
        public string Body { get; set; }
    }
}
