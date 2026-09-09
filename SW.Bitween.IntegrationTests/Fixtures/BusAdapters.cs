namespace SW.Bitween.IntegrationTests.Fixtures;

/// <summary>Adapter ids the fixture installs, so tests and the fixture cannot drift apart.</summary>
public static class BusAdapters
{
    public const string RabbitMq = "bitween.bus.rabbitmq";
    public const string Sqs = "bitween.bus.sqs";

    /// <summary>
    /// Not a bus adapter, and living here anyway because this is where adapter ids are kept. A
    /// relational data source: same resident lifecycle, same supervision, different Kind.
    /// </summary>
    public const string Oracle = "bitween.db.oracle";
    public const string PostgreSql = "bitween.db.postgresql";
}
