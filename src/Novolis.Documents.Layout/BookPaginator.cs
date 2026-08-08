using Novolis.Documents;
using Novolis.Math.Measure;

namespace Novolis.Documents.Layout;

/// <summary>One-column book paginator (dumb layout — not a constraint engine).</summary>
public static class BookPaginator
{
    /// <summary>Builds a <see cref="PagePlan"/> for <paramref name="book"/>.</summary>
    public static PagePlan Paginate(BookDocument book, ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(measurer);

        var pages = new List<PageSlice>();
        var chapterPageNumbers = new Dictionary<string, int>(StringComparer.Ordinal);

        if (book.IncludeCover)
        {
            pages.Add(new PageSlice
            {
                Kind = PageKind.Cover,
                Number = 1,
                Blocks = [new PlacedBlock(new CoverBlock(), 0, 0)],
                ShowHeader = false,
                ShowFooter = false,
            });
        }

        // First pass: body only (no TOC yet) to discover H1 page numbers.
        var bodyStartNumber = pages.Count + 1;
        var bodyPages = PaginateBody(book, measurer, bodyStartNumber, chapterPageNumbers);

        List<TocEntry> tocEntries = [];
        if (book.IncludeToc)
        {
            tocEntries = chapterPageNumbers
                .Select(kv => new TocEntry(kv.Key, kv.Value))
                .OrderBy(e => e.PageNumber)
                .ThenBy(e => e.Title, StringComparer.Ordinal)
                .ToList();

            // Re-paginate: cover → TOC → body (TOC shifts body page numbers).
            pages.Clear();
            chapterPageNumbers.Clear();

            if (book.IncludeCover)
            {
                pages.Add(new PageSlice
                {
                    Kind = PageKind.Cover,
                    Number = 1,
                    Blocks = [new PlacedBlock(new CoverBlock(), 0, 0)],
                    ShowHeader = false,
                    ShowFooter = false,
                });
            }

            var tocPageCount = EstimateTocPageCount(book, measurer, tocEntries);
            var tocStart = pages.Count + 1;
            // Temporary TOC pages; body page numbers assume tocPageCount slots.
            bodyStartNumber = tocStart + tocPageCount;
            bodyPages = PaginateBody(book, measurer, bodyStartNumber, chapterPageNumbers);
            tocEntries = chapterPageNumbers
                .Select(kv => new TocEntry(kv.Key, kv.Value))
                .OrderBy(e => e.PageNumber)
                .ThenBy(e => e.Title, StringComparer.Ordinal)
                .ToList();

            pages.AddRange(BuildTocPages(book, measurer, tocEntries, tocStart));
            // Renumber body pages after actual TOC length (should match estimate for v1).
            var actualBodyStart = pages.Count + 1;
            if (actualBodyStart != bodyStartNumber)
            {
                chapterPageNumbers.Clear();
                bodyPages = PaginateBody(book, measurer, actualBodyStart, chapterPageNumbers);
                tocEntries = chapterPageNumbers
                    .Select(kv => new TocEntry(kv.Key, kv.Value))
                    .OrderBy(e => e.PageNumber)
                    .ThenBy(e => e.Title, StringComparer.Ordinal)
                    .ToList();
                // Rebuild TOC with final numbers (same page count expected).
                pages.RemoveAll(p => p.Kind == PageKind.Toc);
                pages.AddRange(BuildTocPages(book, measurer, tocEntries, tocStart));
            }

            pages.AddRange(bodyPages);
        }
        else
        {
            pages.AddRange(bodyPages);
        }

        if (book.Last is { Lines.Count: > 0 } last)
        {
            var n = pages.Count + 1;
            var blocks = new List<PlacedBlock>();
            float y = 0;
            foreach (var line in last.Lines)
            {
                var style = BodyStyle(book.Typography);
                var h = measurer.MeasureHeight(line, ContentWidth(book), style);
                blocks.Add(new PlacedBlock(new ParagraphBlock { Text = line }, y, h));
                y += h + book.Typography.ParagraphSpacingPt;
            }

            pages.Add(new PageSlice
            {
                Kind = PageKind.Last,
                Number = n,
                Blocks = blocks,
                ShowHeader = false,
                ShowFooter = book.Footer is not null,
            });
        }

        // Normalize numbers
        for (var i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            if (p.Number != i + 1)
            {
                pages[i] = new PageSlice
                {
                    Kind = p.Kind,
                    Number = i + 1,
                    Blocks = p.Blocks,
                    ShowHeader = p.ShowHeader,
                    ShowFooter = p.ShowFooter,
                };
            }
        }

        return new PagePlan { Pages = pages, TocEntries = tocEntries };
    }

    static int EstimateTocPageCount(BookDocument book, ITextMeasurer measurer, IReadOnlyList<TocEntry> entries)
    {
        if (entries.Count == 0)
            return 1;
        return System.Math.Max(1, BuildTocPages(book, measurer, entries, startNumber: 1).Count);
    }

    static List<PageSlice> BuildTocPages(
        BookDocument book,
        ITextMeasurer measurer,
        IReadOnlyList<TocEntry> entries,
        int startNumber)
    {
        var pages = new List<PageSlice>();
        var contentHeight = ContentHeight(book);
        var width = ContentWidth(book);
        var titleStyle = new TextStyle(book.Typography.BodyFontFamily, 16f, book.Typography.LineHeight, Bold: true);
        var lineStyle = BodyStyle(book.Typography);

        var blocks = new List<PlacedBlock>();
        float y = 0;
        var titleH = measurer.MeasureHeight("Contents", width, titleStyle);
        blocks.Add(new PlacedBlock(new HeadingBlock { Level = 2, Text = "Contents" }, y, titleH));
        y += titleH + book.Typography.ParagraphSpacingPt * 2;

        void Flush(int number)
        {
            pages.Add(new PageSlice
            {
                Kind = PageKind.Toc,
                Number = number,
                Blocks = blocks.ToList(),
                ShowHeader = false,
                ShowFooter = book.Footer is not null,
            });
            blocks = [];
            y = 0;
        }

        var pageNumber = startNumber;
        foreach (var entry in entries)
        {
            var text = $"{entry.Title} …… {entry.PageNumber}";
            var h = measurer.MeasureHeight(text, width, lineStyle);
            if (y + h > contentHeight && blocks.Count > 0)
            {
                Flush(pageNumber++);
            }

            blocks.Add(new PlacedBlock(new ParagraphBlock { Text = text }, y, h));
            y += h + book.Typography.ParagraphSpacingPt;
        }

        if (blocks.Count > 0)
            Flush(pageNumber);

        if (pages.Count == 0)
        {
            pages.Add(new PageSlice
            {
                Kind = PageKind.Toc,
                Number = startNumber,
                Blocks = [new PlacedBlock(new HeadingBlock { Level = 2, Text = "Contents" }, 0, titleH)],
                ShowHeader = false,
                ShowFooter = book.Footer is not null,
            });
        }

        return pages;
    }

    static List<PageSlice> PaginateBody(
        BookDocument book,
        ITextMeasurer measurer,
        int startNumber,
        Dictionary<string, int> chapterPageNumbers)
    {
        var pages = new List<PageSlice>();
        var contentHeight = ContentHeight(book);
        var width = ContentWidth(book);
        var blocks = new List<PlacedBlock>();
        float y = 0;
        var pageNumber = startNumber;
        var pageOpensWithH1 = false;

        void Flush(bool opensWithH1)
        {
            if (blocks.Count == 0)
                return;
            pages.Add(new PageSlice
            {
                Kind = PageKind.Body,
                Number = pageNumber,
                Blocks = blocks.ToList(),
                ShowHeader = book.Header is not null
                    && !(book.SuppressHeaderOnChapterOpen && opensWithH1),
                ShowFooter = book.Footer is not null,
            });
            pageNumber++;
            blocks = [];
            y = 0;
            pageOpensWithH1 = false;
        }

        foreach (var block in book.Body)
        {
            switch (block)
            {
                case PageBreakBlock:
                    Flush(pageOpensWithH1);
                    continue;
                case BlankPageBlock:
                    Flush(pageOpensWithH1);
                    pages.Add(new PageSlice
                    {
                        Kind = PageKind.Body,
                        Number = pageNumber++,
                        Blocks = [],
                        ShowHeader = false,
                        ShowFooter = book.Footer is not null,
                    });
                    continue;
                case HeadingBlock { Level: 1 } h1:
                    if (blocks.Count > 0)
                        Flush(pageOpensWithH1);
                    pageOpensWithH1 = true;
                    chapterPageNumbers[h1.Text] = pageNumber;
                    {
                        var style = HeadingStyle(book.Typography, 1);
                        var h = measurer.MeasureHeight(h1.Text, width, style);
                        blocks.Add(new PlacedBlock(h1, y, h));
                        y += h + book.Typography.ParagraphSpacingPt;
                    }
                    continue;
                case CoverBlock or TocBlock:
                    continue;
            }

            var height = MeasureBlock(block, book, measurer, width);
            if (y + height > contentHeight && blocks.Count > 0)
                Flush(pageOpensWithH1);

            if (blocks.Count == 0 && block is HeadingBlock { Level: 1 })
                pageOpensWithH1 = true;

            blocks.Add(new PlacedBlock(block, y, height));
            y += height + book.Typography.ParagraphSpacingPt;
        }

        Flush(pageOpensWithH1);
        return pages;
    }

    static float MeasureBlock(IBlock block, BookDocument book, ITextMeasurer measurer, float width)
    {
        return block switch
        {
            HeadingBlock h => measurer.MeasureHeight(h.Text, width, HeadingStyle(book.Typography, h.Level)),
            ParagraphBlock p => measurer.MeasureHeight(p.Text, width, BodyStyle(book.Typography)),
            SceneBreakBlock s => measurer.MeasureHeight(s.Ornament, width,
                new TextStyle(book.Typography.BodyFontFamily, book.Typography.SceneBreakSizePt, 1f)),
            _ => book.Typography.ParagraphSpacingPt,
        };
    }

    static float ContentWidth(BookDocument book) =>
        book.Setup.Trim.Width.Points - book.Setup.Margin.Horizontal.Points;

    static float ContentHeight(BookDocument book)
    {
        var chrome = book.Setup.HeaderBand.Points + book.Setup.FooterBand.Points;
        return book.Setup.Trim.Height.Points - book.Setup.Margin.Vertical.Points - chrome;
    }

    static TextStyle BodyStyle(Typography t) =>
        new(t.BodyFontFamily, t.BodyFontSizePt, t.LineHeight);

    static TextStyle HeadingStyle(Typography t, int level) =>
        level switch
        {
            1 => new TextStyle(t.BodyFontFamily, t.ChapterTitleSizePt, t.LineHeight, Bold: true),
            2 => new TextStyle(t.BodyFontFamily, t.H2SizePt, t.LineHeight, Bold: true),
            _ => new TextStyle(t.BodyFontFamily, t.H3SizePt, t.LineHeight, Bold: true),
        };
}
