namespace Novolis.Documents;

/// <summary>Optional final page after body flow.</summary>
public sealed class LastPage
{
    /// <summary>Colophon / closing lines (plain paragraphs).</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];
}
