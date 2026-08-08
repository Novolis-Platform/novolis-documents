using Novolis.Documents;
using Novolis.Math.Measure;

namespace Novolis.Documents.Layout;

/// <summary>One-column document paginator (dumb layout — not a constraint engine).</summary>
public static class DocumentPaginator
{
    /// <summary>Builds a <see cref="PagePlan"/> for <paramref name="document"/>.</summary>
    public static PagePlan Paginate(PagedDocument document, ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(measurer);

        var pages = new List<PageSlice>();
        var level1PageNumbers = new Dictionary<string, int>(StringComparer.Ordinal);

        if (document.HasFirstPage)
        {
            var (showHeader, showFooter) = ResolveChrome(document, PageKind.Cover);
            pages.Add(new PageSlice
            {
                Kind = PageKind.Cover,
                Number = 1,
                Blocks = [new PlacedBlock(new CoverBlock(), 0, 0)],
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
        }

        var bodyStartNumber = pages.Count + 1;
        var bodyPages = PaginateBody(document, measurer, bodyStartNumber, level1PageNumbers);

        List<TocEntry> tocEntries = [];
        if (document.IncludeToc)
        {
            tocEntries = level1PageNumbers
                .Select(kv => new TocEntry(kv.Key, kv.Value))
                .OrderBy(e => e.PageNumber)
                .ThenBy(e => e.Title, StringComparer.Ordinal)
                .ToList();

            pages.Clear();
            level1PageNumbers.Clear();

            if (document.HasFirstPage)
            {
                var (showHeader, showFooter) = ResolveChrome(document, PageKind.Cover);
                pages.Add(new PageSlice
                {
                    Kind = PageKind.Cover,
                    Number = 1,
                    Blocks = [new PlacedBlock(new CoverBlock(), 0, 0)],
                    ShowHeader = showHeader,
                    ShowFooter = showFooter,
                });
            }

            var tocPageCount = EstimateTocPageCount(document, measurer, tocEntries);
            var tocStart = pages.Count + 1;
            bodyStartNumber = tocStart + tocPageCount;
            bodyPages = PaginateBody(document, measurer, bodyStartNumber, level1PageNumbers);
            tocEntries = level1PageNumbers
                .Select(kv => new TocEntry(kv.Key, kv.Value))
                .OrderBy(e => e.PageNumber)
                .ThenBy(e => e.Title, StringComparer.Ordinal)
                .ToList();

            pages.AddRange(BuildTocPages(document, measurer, tocEntries, tocStart));
            var actualBodyStart = pages.Count + 1;
            if (actualBodyStart != bodyStartNumber)
            {
                level1PageNumbers.Clear();
                bodyPages = PaginateBody(document, measurer, actualBodyStart, level1PageNumbers);
                tocEntries = level1PageNumbers
                    .Select(kv => new TocEntry(kv.Key, kv.Value))
                    .OrderBy(e => e.PageNumber)
                    .ThenBy(e => e.Title, StringComparer.Ordinal)
                    .ToList();
                pages.RemoveAll(p => p.Kind == PageKind.Toc);
                pages.AddRange(BuildTocPages(document, measurer, tocEntries, tocStart));
            }

            pages.AddRange(bodyPages);
        }
        else
        {
            pages.AddRange(bodyPages);
        }

        if (document.Last is { } last && (last.Lines.Count > 0 || last.Blocks.Count > 0 || !string.IsNullOrWhiteSpace(last.Title)))
            pages.AddRange(BuildLastPages(document, measurer, last, pages.Count + 1));

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

    static int EstimateTocPageCount(PagedDocument document, ITextMeasurer measurer, IReadOnlyList<TocEntry> entries)
    {
        if (entries.Count == 0)
            return 1;
        return System.Math.Max(1, BuildTocPages(document, measurer, entries, startNumber: 1).Count);
    }

    static List<PageSlice> BuildTocPages(
        PagedDocument document,
        ITextMeasurer measurer,
        IReadOnlyList<TocEntry> entries,
        int startNumber)
    {
        var pages = new List<PageSlice>();
        var contentHeight = ContentHeight(document);
        var width = ContentWidth(document);
        var titleStyle = new TextStyle(document.Typography.BodyFontFamily, 16f, document.Typography.LineHeight, Bold: true);
        var lineStyle = BodyStyle(document.Typography);

        var blocks = new List<PlacedBlock>();
        float y = 0;
        var titleH = measurer.MeasureHeight("Contents", width, titleStyle);
        blocks.Add(new PlacedBlock(new HeadingBlock { Level = 2, Text = "Contents" }, y, titleH));
        y += titleH + document.Typography.ParagraphSpacingPt * 2;

        void Flush(int number)
        {
            var (showHeader, showFooter) = ResolveChrome(document, PageKind.Toc);
            pages.Add(new PageSlice
            {
                Kind = PageKind.Toc,
                Number = number,
                Blocks = blocks.ToList(),
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
            blocks.Clear();
            y = 0;
        }

        var pageNumber = startNumber;
        foreach (var entry in entries)
        {
            var text = $"{entry.Title} …… {entry.PageNumber}";
            var h = measurer.MeasureHeight(text, width, lineStyle);
            if (y + h > contentHeight && blocks.Count > 0)
                Flush(pageNumber++);

            blocks.Add(new PlacedBlock(new ParagraphBlock { Text = text }, y, h));
            y += h + document.Typography.ParagraphSpacingPt;
        }

        if (blocks.Count > 0)
            Flush(pageNumber);

        if (pages.Count == 0)
        {
            var (showHeader, showFooter) = ResolveChrome(document, PageKind.Toc);
            pages.Add(new PageSlice
            {
                Kind = PageKind.Toc,
                Number = startNumber,
                Blocks = [new PlacedBlock(new HeadingBlock { Level = 2, Text = "Contents" }, 0, titleH)],
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
        }

        return pages;
    }

    static List<PageSlice> BuildLastPages(
        PagedDocument document,
        ITextMeasurer measurer,
        LastPage last,
        int startNumber)
    {
        var pages = new List<PageSlice>();
        var contentHeight = ContentHeight(document);
        var width = ContentWidth(document);
        var blocks = new List<PlacedBlock>();
        float y = 0;
        var pageNumber = startNumber;
        var typography = document.Typography;

        void Flush()
        {
            if (blocks.Count == 0)
                return;
            var (showHeader, showFooter) = ResolveChrome(document, PageKind.Last);
            pages.Add(new PageSlice
            {
                Kind = PageKind.Last,
                Number = pageNumber++,
                Blocks = blocks.ToList(),
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
            blocks.Clear();
            y = 0;
        }

        if (!string.IsNullOrWhiteSpace(last.Title))
        {
            var style = HeadingStyle(typography, 2);
            var h = measurer.MeasureHeight(last.Title, width, style);
            blocks.Add(new PlacedBlock(new HeadingBlock { Level = 2, Text = last.Title }, y, h));
            y += h + typography.AfterHeadingSpacingPt;
        }

        foreach (var line in last.Lines)
        {
            var style = BodyStyle(typography);
            var h = measurer.MeasureHeight(line, width, style);
            if (y + h > contentHeight && blocks.Count > 0)
                Flush();
            blocks.Add(new PlacedBlock(new ParagraphBlock { Text = line }, y, h));
            y += h + typography.ParagraphSpacingPt;
        }

        foreach (var block in last.Blocks)
        {
            if (block is TableBlock table)
            {
                PlaceTable(table, document, measurer, width, contentHeight, ref y, blocks, Flush);
                continue;
            }

            if (block is ParagraphBlock paragraph)
            {
                PlaceParagraph(paragraph, document, measurer, width, contentHeight, ref y, blocks, Flush);
                continue;
            }

            var height = MeasureBlock(block, document, measurer, width);
            if (y + height > contentHeight && blocks.Count > 0)
                Flush();
            blocks.Add(new PlacedBlock(block, y, height));
            y += height + typography.ParagraphSpacingPt;
        }

        Flush();
        return pages;
    }

    static List<PageSlice> PaginateBody(
        PagedDocument document,
        ITextMeasurer measurer,
        int startNumber,
        Dictionary<string, int> level1PageNumbers)
    {
        var pages = new List<PageSlice>();
        var contentHeight = ContentHeight(document);
        var width = ContentWidth(document);
        var blocks = new List<PlacedBlock>();
        float y = 0;
        var pageNumber = startNumber;
        var pageOpensWithLevel1 = false;
        var typography = document.Typography;

        void Flush()
        {
            if (blocks.Count == 0)
                return;
            var (showHeader, showFooter) = ResolveChrome(document, PageKind.Body);
            if (showHeader && document.SuppressHeaderOnLevel1Open && pageOpensWithLevel1)
                showHeader = false;
            pages.Add(new PageSlice
            {
                Kind = PageKind.Body,
                Number = pageNumber,
                Blocks = blocks.ToList(),
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
            pageNumber++;
            blocks.Clear();
            y = 0;
            pageOpensWithLevel1 = false;
        }

        foreach (var block in document.Body)
        {
            switch (block)
            {
                case PageBreakBlock:
                    Flush();
                    continue;
                case BlankPageBlock:
                    Flush();
                    {
                        var (showHeader, showFooter) = ResolveChrome(document, PageKind.Body);
                        pages.Add(new PageSlice
                        {
                            Kind = PageKind.Body,
                            Number = pageNumber++,
                            Blocks = [],
                            ShowHeader = showHeader,
                            ShowFooter = showFooter,
                        });
                    }
                    continue;
                case HeadingBlock { Level: 1 } h1:
                    if (blocks.Count > 0)
                        Flush();
                    pageOpensWithLevel1 = true;
                    level1PageNumbers[h1.Text] = pageNumber;
                    {
                        var style = HeadingStyle(typography, 1);
                        var h = measurer.MeasureHeight(h1.Text, width, style);
                        blocks.Add(new PlacedBlock(h1, y, h));
                        y += h + typography.AfterLevel1SpacingPt;
                    }
                    continue;
                case CoverBlock or TocBlock:
                    continue;
                case ParagraphBlock paragraph:
                    PlaceParagraph(paragraph, document, measurer, width, contentHeight,
                        ref y, blocks, Flush);
                    continue;
                case TableBlock table:
                    PlaceTable(table, document, measurer, width, contentHeight,
                        ref y, blocks, Flush);
                    continue;
                case ColumnsBlock columns:
                {
                    var height = MeasureBlock(columns, document, measurer, width);
                    if (y + height > contentHeight && blocks.Count > 0)
                        Flush();
                    blocks.Add(new PlacedBlock(columns, y, height));
                    y += height + typography.ParagraphSpacingPt;
                    continue;
                }
                case ImageBlock image:
                {
                    var height = MeasureBlock(image, document, measurer, width);
                    if (y + height > contentHeight && blocks.Count > 0)
                        Flush();
                    blocks.Add(new PlacedBlock(image, y, height));
                    y += height + typography.ParagraphSpacingPt;
                    continue;
                }
                case HeadingBlock heading:
                {
                    var height = MeasureBlock(heading, document, measurer, width);
                    if (y + height > contentHeight && blocks.Count > 0)
                        Flush();
                    blocks.Add(new PlacedBlock(heading, y, height));
                    y += height + (heading.Level == 1
                        ? typography.AfterLevel1SpacingPt
                        : typography.AfterHeadingSpacingPt);
                    continue;
                }
            }

            var blockHeight = MeasureBlock(block, document, measurer, width);
            if (y + blockHeight > contentHeight && blocks.Count > 0)
                Flush();

            blocks.Add(new PlacedBlock(block, y, blockHeight));
            y += blockHeight + typography.ParagraphSpacingPt;
        }

        Flush();
        return pages;
    }

    static void PlaceParagraph(
        ParagraphBlock paragraph,
        PagedDocument document,
        ITextMeasurer measurer,
        float width,
        float contentHeight,
        ref float y,
        List<PlacedBlock> blocks,
        Action flush)
    {
        var style = BodyStyle(document.Typography);
        var lines = measurer.WrapLines(paragraph.Text, width, style);
        if (lines.Count == 0)
            lines = [string.Empty];

        var lineStep = System.Math.Max(1f, style.FontSizePt * style.LineHeight);
        var spacing = document.Typography.ParagraphSpacingPt;
        var index = 0;

        while (index < lines.Count)
        {
            if (y + lineStep > contentHeight && blocks.Count > 0)
                flush();

            var remaining = System.Math.Max(lineStep, contentHeight - y);
            var maxLines = System.Math.Max(1, (int)(remaining / lineStep));
            var take = System.Math.Min(maxLines, lines.Count - index);
            if (take <= 0)
                take = 1;

            var slice = lines.Skip(index).Take(take).ToArray();
            var chunk = slice.Length == 1
                ? slice[0]
                : string.Join(' ', slice.Where(static l => l.Length > 0));

            var height = System.Math.Max(style.FontSizePt, take * lineStep);
            blocks.Add(new PlacedBlock(new ParagraphBlock { Text = chunk }, y, height));
            y += height + spacing;
            index += take;

            if (index < lines.Count)
                flush();
        }
    }

    static void PlaceTable(
        TableBlock table,
        PagedDocument document,
        ITextMeasurer measurer,
        float width,
        float contentHeight,
        ref float y,
        List<PlacedBlock> blocks,
        Action flush)
    {
        var columns = ColumnCount(table);
        if (columns == 0)
            return;

        var typography = document.Typography;
        var padding = typography.TableCellPaddingPt;
        var style = TableStyle(typography);
        var colWidths = ResolveWidths(table.ColumnWidths, columns, width);
        var showHeader = table.ShowHeader && table.Headers.Count > 0;
        var headerHeight = showHeader
            ? MeasureTableRow(table.Headers, columns, colWidths, padding, style, measurer)
            : 0f;

        var rowIndex = 0;
        var isFirstSlice = true;
        while (rowIndex < table.Rows.Count || (isFirstSlice && showHeader && table.Rows.Count == 0))
        {
            var includeHeader = showHeader && (isFirstSlice || table.RepeatHeaderOnPageBreak);
            var used = includeHeader ? headerHeight : 0f;
            if (y + used + typography.ParagraphSpacingPt > contentHeight && blocks.Count > 0)
            {
                flush();
                includeHeader = showHeader && table.RepeatHeaderOnPageBreak;
                used = includeHeader ? headerHeight : 0f;
            }

            var sliceRows = new List<IReadOnlyList<string>>();
            while (rowIndex < table.Rows.Count)
            {
                var rowH = MeasureTableRow(table.Rows[rowIndex], columns, colWidths, padding, style, measurer);
                if (used + rowH > contentHeight - y && sliceRows.Count > 0)
                    break;
                if (used + rowH > contentHeight - y && sliceRows.Count == 0 && blocks.Count > 0)
                {
                    flush();
                    includeHeader = showHeader && table.RepeatHeaderOnPageBreak;
                    used = includeHeader ? headerHeight : 0f;
                    continue;
                }

                sliceRows.Add(table.Rows[rowIndex]);
                used += rowH;
                rowIndex++;
            }

            var slice = new TableBlock
            {
                Headers = includeHeader ? table.Headers : [],
                Rows = sliceRows,
                ColumnWidths = table.ColumnWidths,
                ColumnAlignments = table.ColumnAlignments,
                ShowHeader = includeHeader,
                RuleStyle = table.RuleStyle,
                HeaderBackground = table.HeaderBackground,
                RepeatHeaderOnPageBreak = table.RepeatHeaderOnPageBreak,
            };
            blocks.Add(new PlacedBlock(slice, y, used));
            y += used + typography.ParagraphSpacingPt;
            isFirstSlice = false;

            if (rowIndex < table.Rows.Count)
                flush();
            else
                break;
        }
    }

    static int ColumnCount(TableBlock table)
    {
        var n = table.Headers.Count;
        foreach (var row in table.Rows)
            n = System.Math.Max(n, row.Count);
        return n;
    }

    static float[] ResolveWidths(IReadOnlyList<float>? fractions, int count, float totalWidth)
    {
        var widths = new float[count];
        if (count <= 0)
            return widths;

        if (fractions is null || fractions.Count != count)
        {
            var equal = totalWidth / count;
            for (var i = 0; i < count; i++)
                widths[i] = equal;
            return widths;
        }

        float sum = 0f;
        for (var i = 0; i < count; i++)
            sum += System.Math.Max(0f, fractions[i]);
        if (sum <= 0f)
        {
            var equal = totalWidth / count;
            for (var i = 0; i < count; i++)
                widths[i] = equal;
            return widths;
        }

        for (var i = 0; i < count; i++)
            widths[i] = totalWidth * System.Math.Max(0f, fractions[i]) / sum;
        return widths;
    }

    static float MeasureTableRow(
        IReadOnlyList<string> cells,
        int columns,
        float[] colWidths,
        float padding,
        TextStyle style,
        ITextMeasurer measurer)
    {
        float max = style.FontSizePt * style.LineHeight;
        for (var c = 0; c < columns; c++)
        {
            var text = c < cells.Count ? cells[c] : string.Empty;
            var textWidth = System.Math.Max(8f, colWidths[c] - padding * 2f);
            var h = measurer.MeasureHeight(text, textWidth, style);
            if (h > max)
                max = h;
        }

        return max + padding * 2f;
    }

    static float MeasureBlock(IBlock block, PagedDocument document, ITextMeasurer measurer, float width)
    {
        return block switch
        {
            HeadingBlock h => measurer.MeasureHeight(h.Text, width, HeadingStyle(document.Typography, h.Level)),
            ParagraphBlock p => measurer.MeasureHeight(p.Text, width, BodyStyle(document.Typography)),
            TableBlock t => MeasureTable(t, document, measurer, width),
            ImageBlock image => System.Math.Max(0f, image.HeightPt),
            ColumnsBlock columns => MeasureColumns(columns, document, measurer, width),
            SceneBreakBlock s => measurer.MeasureHeight(s.Ornament, width,
                new TextStyle(document.Typography.BodyFontFamily, document.Typography.SceneBreakSizePt, 1f)),
            _ => document.Typography.ParagraphSpacingPt,
        };
    }

    static float MeasureColumns(ColumnsBlock columns, PagedDocument document, ITextMeasurer measurer, float width)
    {
        if (columns.Columns.Count == 0)
            return 0f;

        var gap = System.Math.Max(0f, columns.GapPt);
        var usable = System.Math.Max(0f, width - gap * (columns.Columns.Count - 1));
        var colWidths = ResolveWidths(columns.Fractions, columns.Columns.Count, usable);
        float max = 0f;
        for (var i = 0; i < columns.Columns.Count; i++)
        {
            float h = 0f;
            foreach (var child in columns.Columns[i])
            {
                h += MeasureBlock(child, document, measurer, colWidths[i]);
                h += child is HeadingBlock heading
                    ? (heading.Level == 1
                        ? document.Typography.AfterLevel1SpacingPt
                        : document.Typography.AfterHeadingSpacingPt)
                    : document.Typography.ParagraphSpacingPt;
            }

            if (h > max)
                max = h;
        }

        return max;
    }

    static float MeasureTable(TableBlock table, PagedDocument document, ITextMeasurer measurer, float width)
    {
        var columns = ColumnCount(table);
        if (columns == 0)
            return 0f;
        var padding = document.Typography.TableCellPaddingPt;
        var style = TableStyle(document.Typography);
        var colWidths = ResolveWidths(table.ColumnWidths, columns, width);
        float total = 0f;
        if (table.ShowHeader && table.Headers.Count > 0)
            total += MeasureTableRow(table.Headers, columns, colWidths, padding, style, measurer);
        foreach (var row in table.Rows)
            total += MeasureTableRow(row, columns, colWidths, padding, style, measurer);
        return total;
    }

    static (bool ShowHeader, bool ShowFooter) ResolveChrome(PagedDocument document, PageKind kind)
    {
        var chrome = document.Chrome ?? ChromeOptions.Default;
        var band = kind switch
        {
            PageKind.Cover => chrome.First,
            PageKind.Toc => chrome.Toc,
            PageKind.Last => chrome.Last,
            _ => chrome.Body,
        };
        return (
            ChromeOptions.HasHeader(band) && document.Header is not null,
            ChromeOptions.HasFooter(band) && document.Footer is not null);
    }

    static float ContentWidth(PagedDocument document) =>
        document.Setup.Trim.Width.Points - document.Setup.Margin.Horizontal.Points;

    static float ContentHeight(PagedDocument document)
    {
        var chrome = document.Setup.HeaderBand.Points + document.Setup.FooterBand.Points;
        return document.Setup.Trim.Height.Points - document.Setup.Margin.Vertical.Points - chrome;
    }

    static TextStyle BodyStyle(Typography t) =>
        new(t.BodyFontFamily, t.BodyFontSizePt, t.LineHeight);

    static TextStyle TableStyle(Typography t) =>
        new(t.BodyFontFamily, t.EffectiveTableFontSizePt, t.LineHeight);

    static TextStyle HeadingStyle(Typography t, int level) =>
        level switch
        {
            1 => new TextStyle(t.BodyFontFamily, t.H1SizePt, t.LineHeight, Bold: true),
            2 => new TextStyle(t.BodyFontFamily, t.H2SizePt, t.LineHeight, Bold: true),
            _ => new TextStyle(t.BodyFontFamily, t.H3SizePt, t.LineHeight, Bold: true),
        };
}
