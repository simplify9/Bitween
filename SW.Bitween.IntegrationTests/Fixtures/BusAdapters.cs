namespace SW.Bitween.IntegrationTests.Fixtures;

/// <summary>Adapter ids the fixture installs, so tests and the fixture cannot drift apart.</summary>
public static class BusAdapters
{
    public const string RabbitMq = "bitween.bus.rabbitmq";
    public const string Sqs = "bitween.bus.sqs";
}
