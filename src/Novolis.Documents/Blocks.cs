namespace Novolis.Documents;

/// <summary>Cover page content (first page).</summary>
public sealed class CoverBlock : IBlock
{
    /// <summary>Cover uses <see cref="BookMeta"/> from the document; this marker is optional in Body.</summary>
    public bool Present { get; init; } = true;
}

/// <summary>Table of contents placeholder; entries are filled by layout from H1 titles.</summary>
public sealed class TocBlock : IBlock
{
    /// <summary>Optional pre-supplied entries; when empty, layout collects H1 headings.</summary>
    public IReadOnlyList<TocEntry> Entries { get; init; } = [];
}

/// <summary>One TOC line.</summary>
/// <param name="Title">Chapter title.</param>
/// <param name="PageNumber">1-based page number within the finished plan (0 until layout fills).</param>
public sealed record TocEntry(string Title, int PageNumber = 0);

/// <summary>Heading levels 1–3. H1 forces a page break when prior content exists.</summary>
public sealed class HeadingBlock : IBlock
{
    /// <summary>1 = chapter, 2 = section, 3 = subsection.</summary>
    public required int Level { get; init; }

    /// <summary>Heading text.</summary>
    public required string Text { get; init; }
}

/// <summary>Plain paragraph (inlines already resolved by the consumer).</summary>
public sealed class ParagraphBlock : IBlock
{
    /// <summary>Paragraph text.</summary>
    public required string Text { get; init; }
}

/// <summary>Centered scene-break ornament.</summary>
public sealed class SceneBreakBlock : IBlock
{
    /// <summary>Glyph or short ornament string.</summary>
    public string Ornament { get; init; } = "***";
}

/// <summary>Explicit page break.</summary>
public sealed class PageBreakBlock : IBlock;

/// <summary>Blank page (optional verso/recto spacer).</summary>
public sealed class BlankPageBlock : IBlock;
