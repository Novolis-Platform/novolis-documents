namespace Novolis.Documents;

/// <summary>Cover page marker (first page). Prefer <see cref="PagedDocument.First"/> / <see cref="PagedDocument.IncludeCover"/>.</summary>
public sealed class CoverBlock : IBlock
{
    /// <summary>Marker only; title-page content comes from <see cref="DocumentMeta"/> / <see cref="FirstPage"/>.</summary>
    public bool Present { get; init; } = true;
}
