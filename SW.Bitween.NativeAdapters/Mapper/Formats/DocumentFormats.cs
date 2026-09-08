namespace SW.Bitween.NativeAdapters.Mapper.Formats;

/// <summary>
/// The formats a mapping can read and write, by the id stored in its rules.
/// </summary>
/// <remarks>
/// One registry, so the adapter and the preview endpoint cannot disagree about what is supported —
/// a format the editor offers but the pipeline rejects would be a mapping that previews and then
/// fails. Adding a format is one entry here plus the reader and writer it names.
/// </remarks>
public static class DocumentFormats
{
    private static readonly Dictionary<string, IDocumentFormat> ById =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["json"] = new JsonFormat(),
        };

    /// <summary>Format ids, for the editor's dropdown and for error messages.</summary>
    public static IReadOnlyList<string> Ids { get; } = ById.Keys.OrderBy(k => k).ToList();

    public static bool TryGet(string? id, out IDocumentFormat? format)
    {
        format = null;
        return id is not null && ById.TryGetValue(id, out format);
    }

    /// <summary>
    /// The message for an id no format answers to, naming what is available.
    /// </summary>
    /// <param name="role">Either <c>source</c> or <c>target</c>, so the reader knows which end.</param>
    public static string Unsupported(string? id, string role) =>
        $"'{id}' is not a {role} format this mapper supports. Supported: {string.Join(", ", Ids)}.";
}
