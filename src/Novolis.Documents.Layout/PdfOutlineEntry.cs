namespace Novolis.Documents.Layout;

/// <summary>One outline bookmark: title, destination page, and nested children.</summary>
/// <param name="Title">Bookmark title shown in a reader sidebar.</param>
/// <param name="PageNumber">1-based page number in the finished plan.</param>
/// <param name="Children">Nested bookmarks under this entry.</param>
public sealed record PdfOutlineEntry(
    string Title,
    int PageNumber,
    IReadOnlyList<PdfOutlineEntry>? Children = null);
