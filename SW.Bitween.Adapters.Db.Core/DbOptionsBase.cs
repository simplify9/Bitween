namespace SW.Bitween.Adapters.Db;

/// <summary>
/// The settings every relational adapter accepts, whatever the engine. Each concrete adapter
/// derives from this and adds its own connection fields, then marks its class
/// <c>[AdapterSettings(Kind = "Relational", …)]</c> so Bitween can build the form from it.
///
/// Everything here arrives as a DataSource property, bound by name.
/// </summary>
public abstract class DbOptionsBase
{
    // ------------------------------------------------------------------ pooling

    /// <summary>
    /// The reason this adapter is resident at all. An ephemeral adapter gets a cold pool per
    /// process, so every Xchange pays a TCP connect, a TLS handshake and an authentication round
    /// trip — 20–150 ms to a remote database, and considerably more to Oracle. A process that
    /// stays up pays it once.
    /// </summary>
    [AdapterSetting(Default = "2", Hint =
        "Connections kept open even while idle. Above zero, a scheduled job at 3am runs instead of "
        + "timing out reconnecting. Costs one server session per connection, per node.")]
    public int MinPoolSize { get; set; } = 2;

    [AdapterSetting(Default = "20", Hint =
        "Ceiling on concurrent connections FROM THIS NODE. Multiply by your replica count before "
        + "comparing it to the server's session limit.")]
    public int MaxPoolSize { get; set; } = 20;

    [AdapterSetting(Default = "15", Hint = "Seconds to wait for a connection before giving up.")]
    public int ConnectTimeoutSeconds { get; set; } = 15;

    // ------------------------------------------------------------------ execution

    [AdapterSetting(Default = "30", Hint =
        "Default seconds a statement may run. A statement can raise its own; this is the ceiling "
        + "for anything that does not.")]
    public int CommandTimeoutSeconds { get; set; } = 30;

    [AdapterSetting(Default = "1000", Hint =
        "Rows a single query may return. Exceeding it fails with a message pointing at the paging "
        + "commands rather than quietly handing back a truncated answer.")]
    public int MaxRows { get; set; } = 1000;

    [AdapterSetting(Default = "60", Hint =
        "Seconds an unread paging cursor is kept before it is closed. A cursor holds a pooled "
        + "connection open, so an abandoned one is a connection nobody can use.")]
    public int CursorIdleTimeoutSeconds { get; set; } = 60;

    // ------------------------------------------------------------------ safety

    /// <summary>
    /// Named statements as JSON: <c>{"getOrder": "select * from orders where id = :id"}</c>.
    /// The message supplies parameter values; it never supplies SQL.
    ///
    /// Composed by Bitween from the data source's statement rows, not typed by an operator — which
    /// is why it is hidden. Statements are their own entity so that writing a query and changing a
    /// database password are different permissions; the adapter still just receives name-to-SQL and
    /// knows nothing about where they were kept.
    /// </summary>
    [AdapterSetting(Hidden = true, Hint =
        "Named statements as JSON — {\"name\":\"select …\"}. Composed by Bitween from the data "
        + "source's statements; not edited here.")]
    public string Statements { get; set; }

    [AdapterSetting(Default = "false", AllowedValues = new[] { "true", "false" }, Hint =
        "Lets a caller send SQL text instead of naming a configured statement. Off, and it should "
        + "stay off: a mapper is a template over message content, so SQL it can emit is SQL an "
        + "inbound message can steer.")]
    public bool AllowAdHocSql { get; set; }

    [AdapterSetting(Default = "false", AllowedValues = new[] { "true", "false" }, Hint =
        "Writes parameter VALUES into the adapter log at Debug level. Off: trace-logging a payment "
        + "insert is a data-protection incident wearing a debug flag. For a controlled test only.")]
    public bool LogParameterValues { get; set; }

    // ------------------------------------------------------------------ receiving

    [AdapterSetting(
        AllowedValues = new[] { "bulk", "incrementing", "timestamp", "timestamp+incrementing", "marker" },
        Hint =
        "How the receiver finds new rows. bulk re-reads everything each poll; incrementing follows "
        + "an always-growing column; timestamp follows a modified-at column; marker reads rows a "
        + "flag says are unprocessed. None of them can see a DELETE.")]
    public string ReceiveMode { get; set; }

    [AdapterSetting(Hint =
        "The statement the receiver polls with. Reference the cursor as :cursor — it is bound from "
        + "the last value consumed. Order by the cursor column, or rows will be skipped.")]
    public string ReceiveStatement { get; set; }

    [AdapterSetting(Hint =
        "The column carrying the cursor: the incrementing id, or the timestamp. Its value in the "
        + "last row read is what gets saved.")]
    public string CursorColumn { get; set; }

    [AdapterSetting(Hint =
        "The primary key column, used to identify a row for mark-processed and for deduplication.")]
    public string KeyColumn { get; set; }

    [AdapterSetting(Hint =
        "Run against each row once Bitween has accepted it — Camel's onConsume: set a flag, move "
        + "the row, delete it. Bind the row's key as :key. Required for marker mode, and the only "
        + "thing that stops bulk mode reading the same rows forever.")]
    public string MarkProcessedStatement { get; set; }

    [AdapterSetting(Default = "500", Hint = "Rows one poll may take. The next poll takes the next batch.")]
    public int ReceiveBatchSize { get; set; } = 500;
}
