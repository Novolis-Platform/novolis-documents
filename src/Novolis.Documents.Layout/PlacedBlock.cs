using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>A block placed on a page at a vertical offset within the content box.</summary>
/// <param name="Block">Source block.</param>
/// <param name="YInContentPt">Y offset from the top of the content rect.</param>
/// <param name="HeightPt">Occupied height.</param>
public sealed record PlacedBlock(IBlock Block, float YInContentPt, float HeightPt);
