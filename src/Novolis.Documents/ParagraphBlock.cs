namespace Novolis.Documents;

/// <summary>Plain paragraph (inlines already resolved by the consumer).</summary>
public sealed class ParagraphBlock : IBlock
{
    /// <summary>Paragraph text.</summary>
    public required string Text { get; init; }
}
