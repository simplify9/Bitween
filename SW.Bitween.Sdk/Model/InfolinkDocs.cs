namespace SW.Bitween.Model;

public class GetBitweenDocModel
{
    /// <summary>Required; the server rejects a request without it.</summary>
    public string DocumentKey { get; set; } = null!;
}