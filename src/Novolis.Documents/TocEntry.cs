namespace Novolis.Documents;

/// <summary>One contents line.</summary>
/// <param name="Title">Heading title.</param>
/// <param name="PageNumber">1-based page number within the finished plan (0 until layout fills).</param>
public sealed record TocEntry(string Title, int PageNumber = 0);
