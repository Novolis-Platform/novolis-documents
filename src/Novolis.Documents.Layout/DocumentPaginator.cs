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
            pages.AddRange(BuildFirstPages(document, measurer, startNumber: 1));

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
                pages.AddRange(BuildFirstPages(document, measurer, startNumber: 1));

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
                    ChapterTitle = p.ChapterTitle,
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
            var (showHeader, showFooter) = ResolveBands(document, PageKind.Toc);
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
            var (showHeader, showFooter) = ResolveBands(document, PageKind.Toc);
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

    static List<PageSlice> BuildFirstPages(PagedDocument document, ITextMeasurer measurer, int startNumber)
    {
        var meta = document.Meta;
        var first = document.First;
        var flow = new List<IBlock>
        {
            new HeadingBlock { Level = 1, Text = first?.Title ?? meta.Title },
        };

        void AddLine(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                flow.Add(new ParagraphBlock { Text = text });
        }

        AddLine(first?.Subtitle ?? meta.Subtitle);
        AddLine(first?.Series ?? meta.Series);
        AddLine(first?.Author ?? meta.Author);
        AddLine(meta.Contributors);
        AddLine(meta.Publisher);
        if (meta.Date is { } date)
            AddLine(date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(meta.Version))
            AddLine($"Version {meta.Version}");
        AddLine(meta.Identifier);
        AddLine(first?.Rights ?? meta.Rights);

        if (first is not null)
        {
            foreach (var line in first.Lines)
                AddLine(line);
            flow.AddRange(first.Blocks);
        }

        return PaginateRegion(document, measurer, flow, PageKind.Cover, startNumber);
    }

    static List<PageSlice> BuildLastPages(
        PagedDocument document,
        ITextMeasurer measurer,
        LastPage last,
        int startNumber)
    {
        var flow = new List<IBlock>();
        if (!string.IsNullOrWhiteSpace(last.Title))
            flow.Add(new HeadingBlock { Level = 2, Text = last.Title });
        foreach (var line in last.Lines)
            flow.Add(new ParagraphBlock { Text = line });
        flow.AddRange(last.Blocks);
        return PaginateRegion(document, measurer, flow, PageKind.Last, startNumber);
    }

    /// <summary>
    /// Paginates First/Last (or similar) flow. One page when content fits; continues when it overflows.
    /// </summary>
    static List<PageSlice> PaginateRegion(
        PagedDocument document,
        ITextMeasurer measurer,
        IReadOnlyList<IBlock> flow,
        PageKind kind,
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
            var (showHeader, showFooter) = ResolveBands(document, kind);
            pages.Add(new PageSlice
            {
                Kind = kind,
                Number = pageNumber++,
                Blocks = blocks.ToList(),
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
            blocks.Clear();
            y = 0;
        }

        foreach (var block in flow)
        {
            switch (block)
            {
                case PageBreakBlock:
                    Flush();
                    continue;
                case LineBreakBlock lineBreak:
                {
                    var height = MeasureBlock(lineBreak, document, measurer, width);
                    if (y + height > contentHeight && blocks.Count > 0)
                        Flush();
                    blocks.Add(new PlacedBlock(lineBreak, y, height));
                    y += height;
                    continue;
                }
                case BlankPageBlock:
                    Flush();
                    {
                        var (showHeader, showFooter) = ResolveBands(document, kind);
                        pages.Add(new PageSlice
                        {
                            Kind = kind,
                            Number = pageNumber++,
                            Blocks = [],
                            ShowHeader = showHeader,
                            ShowFooter = showFooter,
                        });
                    }
                    continue;
                case ParagraphBlock paragraph:
                    PlaceParagraph(paragraph, document, measurer, width, contentHeight, ref y, blocks, Flush);
                    continue;
                case TableBlock table:
                    PlaceTable(table, document, measurer, width, contentHeight, ref y, blocks, Flush);
                    continue;
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
        if (pages.Count == 0)
        {
            var (showHeader, showFooter) = ResolveBands(document, kind);
            pages.Add(new PageSlice
            {
                Kind = kind,
                Number = startNumber,
                Blocks = [],
                ShowHeader = showHeader,
                ShowFooter = showFooter,
            });
        }

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
        string? chapterTitle = null;
        var typography = document.Typography;

        void Flush()
        {
            if (blocks.Count == 0)
                return;
            var (showHeader, showFooter) = ResolveBands(document, PageKind.Body);
            pages.Add(new PageSlice
            {
                Kind = PageKind.Body,
                Number = pageNumber,
                Blocks = blocks.ToList(),
                ShowHeader = showHeader,
                ShowFooter = showFooter,
                ChapterTitle = chapterTitle,
            });
            pageNumber++;
            blocks.Clear();
            y = 0;
        }

        foreach (var block in document.Body)
        {
            switch (block)
            {
                case PageBreakBlock:
                    Flush();
                    continue;
                case LineBreakBlock lineBreak:
                {
                    var height = MeasureBlock(lineBreak, document, measurer, width);
                    if (y + height > contentHeight && blocks.Count > 0)
                        Flush();
                    blocks.Add(new PlacedBlock(lineBreak, y, height));
                    y += height;
                    continue;
                }
                case BlankPageBlock:
                    Flush();
                    {
                        var (showHeader, showFooter) = ResolveBands(document, PageKind.Body);
                        pages.Add(new PageSlice
                        {
                            Kind = PageKind.Body,
                            Number = pageNumber++,
                            Blocks = [],
                            ShowHeader = showHeader,
                            ShowFooter = showFooter,
                            ChapterTitle = chapterTitle,
                        });
                    }
                    continue;
                case HeadingBlock { Level: 1 } h1:
                    if (blocks.Count > 0)
                        Flush();
                    chapterTitle = h1.Text;
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
        var placedAnySlice = false;
        while (rowIndex < table.Rows.Count || (!placedAnySlice && showHeader && table.Rows.Count == 0))
        {
            // Keep the header on the first emitted slice even when we had to flush
            // leftover page content before the table could start.
            bool IncludeHeader() =>
                showHeader && (!placedAnySlice || table.RepeatHeaderOnPageBreak);

            var includeHeader = IncludeHeader();
            var used = includeHeader ? headerHeight : 0f;
            if (y + used > contentHeight && blocks.Count > 0)
            {
                flush();
                includeHeader = IncludeHeader();
                used = includeHeader ? headerHeight : 0f;
            }

            var sliceRows = new List<IReadOnlyList<string>>();
            while (rowIndex < table.Rows.Count)
            {
                var rowH = MeasureTableRow(table.Rows[rowIndex], columns, colWidths, padding, style, measurer);
                var remaining = contentHeight - y;
                if (used + rowH > remaining && sliceRows.Count > 0)
                    break;

                if (used + rowH > remaining && sliceRows.Count == 0)
                {
                    // Header (optional) fits alone but the next row does not — move on
                    // when there is prior content; otherwise accept an oversized row so
                    // pagination cannot stall.
                    if (blocks.Count > 0)
                    {
                        flush();
                        includeHeader = IncludeHeader();
                        used = includeHeader ? headerHeight : 0f;
                        continue;
                    }

                    sliceRows.Add(table.Rows[rowIndex]);
                    used += rowH;
                    rowIndex++;
                    break;
                }

                sliceRows.Add(table.Rows[rowIndex]);
                used += rowH;
                rowIndex++;
            }

            // Header-only slice when the table has no body rows.
            if (sliceRows.Count == 0 && !(includeHeader && table.Rows.Count == 0))
            {
                if (blocks.Count > 0)
                {
                    flush();
                    continue;
                }

                // Empty page but nothing fits (degenerate header taller than page): emit header alone.
                if (!includeHeader)
                    break;
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
            placedAnySlice = true;

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
            LineBreakBlock => document.Typography.BodyFontSizePt * document.Typography.LineHeight,
            PageBreakBlock or BlankPageBlock => 0f,
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

    static (bool ShowHeader, bool ShowFooter) ResolveBands(PagedDocument document, PageKind kind)
    {
        var header = document.Header;
        var footer = document.Footer;
        var showHeader = header is not null && kind switch
        {
            PageKind.Cover => header.IncludeFirstPage,
            PageKind.Toc => header.IncludeToc,
            PageKind.Last => header.IncludeLastPage,
            _ => header.IncludeBody,
        };
        var showFooter = footer is not null && kind switch
        {
            PageKind.Cover => footer.IncludeFirstPage,
            PageKind.Toc => footer.IncludeToc,
            PageKind.Last => footer.IncludeLastPage,
            _ => footer.IncludeBody,
        };
        return (showHeader, showFooter);
    }

    static float ContentWidth(PagedDocument document) =>
        document.Setup.Trim.Width.Points - document.Setup.Margin.Horizontal.Points;

    static float ContentHeight(PagedDocument document)
    {
        var bands = document.Setup.HeaderBand.Points + document.Setup.FooterBand.Points;
        return document.Setup.Trim.Height.Points - document.Setup.Margin.Vertical.Points - bands;
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
