namespace Novolis.Documents;

/// <summary>Heading levels 1–4. Level 1 forces a page break when prior content exists.</summary>
public sealed class HeadingBlock : IBlock
{
    /// <summary>1 = top-level section, 2–4 = nested headings.</summary>
    public required int Level { get; init; }

    /// <summary>Heading text.</summary>
    public required string Text { get; init; }
}
