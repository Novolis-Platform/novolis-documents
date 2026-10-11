using System.Globalization;
using Novolis.Documents;

namespace Novolis.Documents.Layout;

/// <summary>
/// Paginates a semantic document into explicit renderer-independent fragments.
/// </summary>
public static class SectionPaginator
{
    /// <summary>Builds a page plan using the supplied deterministic text measurer.</summary>
    public static PagePlan Paginate(Document document, ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(measurer);

        var width = document.Setup.Trim.Width.Points - document.Setup.Margin.Horizontal.Points;
        var height = document.Setup.Trim.Height.Points
            - document.Setup.Margin.Vertical.Points
            - document.Setup.HeaderBand.Points
            - document.Setup.FooterBand.Points;
        if (width <= 0f || height <= 0f)
        {
            throw new LayoutFailure(
                LayoutFailureKind.NoUsablePageArea,
                $"The page has no usable content area ({width:0.##} × {height:0.##} points).",
                availableWidthPt: width,
                availableHeightPt: height);
        }

        var pages = new List<PageSlice>();
        var outline = new List<TocEntry>();

        if (document.FirstPage is { Sections.Count: > 0 } first)
            PaginateRegion(document, first.Sections, PageKind.First, width, height, measurer, pages, outline);

        var bodySections = document.Sections;
        if (bodySections.Count > 0)
            PaginateRegion(document, bodySections, PageKind.Body, width, height, measurer, pages, outline);
        else if (pages.Count == 0 && document.LastPage is null)
            pages.Add(CreatePage(PageKind.Body, 1));

        if (document.LastPage is { Sections.Count: > 0 } last)
            PaginateRegion(document, last.Sections, PageKind.Last, width, height, measurer, pages, outline);

        if (pages.Count == 0)
            pages.Add(CreatePage(PageKind.Body, 1));

        foreach (var page in pages)
        {
            page.ShowHeader = HeaderApplies(document.Header, page.Kind);
            page.ShowFooter = FooterApplies(document.Footer, page.Kind);
        }

        return new PagePlan
        {
            Pages = pages,
            TocEntries = document.IncludeToc ? outline : [],
        };
    }

    static void PaginateRegion(
        Document document,
        IReadOnlyList<ISection> sourceSections,
        PageKind kind,
        float width,
        float pageHeight,
        ITextMeasurer measurer,
        List<PageSlice> pages,
        List<TocEntry> outline)
    {
        var pending = new Queue<ISection>(sourceSections);
        var page = CreatePage(kind, pages.Count + 1);
        pages.Add(page);
        var y = 0f;

        while (pending.Count > 0)
        {
            var section = pending.Dequeue();
            if (section is PageBreakSection)
            {
                if (page.Fragments.Count > 0 || page.Blocks.Count > 0)
                {
                    page = CreatePage(kind, pages.Count + 1);
                    pages.Add(page);
                }

                y = 0f;
                continue;
            }

            if (page.Fragments.Count > 0
                && GetLayout(section).KeepWithNext
                && pending.TryPeek(out var next)
                && ShouldMoveWithNext(
                    document,
                    section,
                    next,
                    width,
                    pageHeight,
                    y,
                    measurer))
            {
                page = CreatePage(kind, pages.Count + 1);
                pages.Add(page);
                y = 0f;
            }

            var continuation = false;
            while (true)
            {
                var remaining = pageHeight - y;
                var result = LayoutSection(
                    document,
                    section,
                    width,
                    remaining,
                    pageHeight,
                    measurer,
                    continuation);

                switch (result)
                {
                    case SectionLayoutResult.Fits fits:
                        Place(page, fits.Fragment, y, outline);
                        y += fits.Fragment.HeightPt;
                        y += SpaceAfter(section);
                        break;

                    case SectionLayoutResult.Split split:
                        Place(page, split.Head, y, outline);
                        y += split.Head.HeightPt;
                        page = CreatePage(kind, pages.Count + 1);
                        pages.Add(page);
                        y = 0f;
                        section = split.Tail;
                        continuation = true;
                        continue;

                    case SectionLayoutResult.NeedsNewPage:
                        if (page.Fragments.Count == 0 && page.Blocks.Count == 0)
                        {
                            throw new LayoutFailure(
                                LayoutFailureKind.UnsatisfiableKeepTogether,
                                $"Section {section.GetType().Name} cannot fit on an empty page.",
                                section.GetType(),
                                width,
                                pageHeight);
                        }

                        page = CreatePage(kind, pages.Count + 1);
                        pages.Add(page);
                        y = 0f;
                        continuation = false;
                        continue;

                    case SectionLayoutResult.Impossible impossible:
                        throw impossible.Failure;
                }

                break;
            }
        }
    }

    static SectionLayoutResult LayoutSection(
        Document document,
        ISection section,
        float width,
        float remainingHeight,
        float pageHeight,
        ITextMeasurer measurer,
        bool continuation)
    {
        var layout = GetLayout(section);
        var before = continuation ? 0f : System.Math.Max(0f, layout.SpaceBefore);
        var after = System.Math.Max(0f, layout.SpaceAfter);
        var available = remainingHeight - before;
        if (available <= 0f)
            return new SectionLayoutResult.NeedsNewPage();

        return section switch
        {
            Section flow => LayoutFlow(
                document,
                flow,
                width,
                available,
                pageHeight,
                measurer,
                before,
                after),
            ColumnsSection columns => LayoutColumns(
                document,
                columns,
                width,
                available,
                pageHeight,
                measurer,
                before),
            TableSection table => LayoutTable(
                document,
                table,
                width,
                available,
                pageHeight,
                measurer,
                continuation,
                before),
            Box box => LayoutBox(
                document,
                box,
                width,
                available,
                pageHeight,
                measurer,
                before),
            _ => new SectionLayoutResult.Impossible(new LayoutFailure(
                LayoutFailureKind.UnsupportedContent,
                $"No section layout exists for {section.GetType().FullName}.",
                section.GetType(),
                width,
                available)),
        };
    }

    static SectionLayoutResult LayoutFlow(
        Document document,
        Section section,
        float width,
        float availableHeight,
        float pageHeight,
        ITextMeasurer measurer,
        float before,
        float after)
    {
        var fullHeight = MeasureContents(document, section.Contents, width, measurer)
            + before
            + after;
        if (fullHeight <= availableHeight)
        {
            return new SectionLayoutResult.Fits(new FlowFragment(
                section,
                section.Contents,
                fullHeight - after));
        }

        if (section.Layout.KeepTogether && fullHeight <= pageHeight)
            return new SectionLayoutResult.NeedsNewPage();

        var head = new List<IContent>();
        var used = before;
        for (var index = 0; index < section.Contents.Count; index++)
        {
            var content = section.Contents[index];
            var itemHeight = MeasureContent(document, content, width, measurer);
            var separator = head.Count == 0 ? 0f : ParagraphSpacing(document);
            if (used + separator + itemHeight + after <= availableHeight)
            {
                head.Add(content);
                used += separator + itemHeight;
                continue;
            }

            if (content is Text text)
            {
                var textWidth = System.Math.Max(8f, width);
                var lines = measurer.WrapLines(text.Value, textWidth, TextStyleFor(document, text.Style));
                var lineHeight = System.Math.Max(
                    TextStyleFor(document, text.Style).FontSizePt,
                    TextStyleFor(document, text.Style).FontSizePt * document.Typography.LineHeight);
                var room = System.Math.Max(0f, availableHeight - used - separator - after);
                var lineCount = System.Math.Min(lines.Count, System.Math.Max(0, (int)System.Math.Floor(room / lineHeight)));
                if (lineCount > 0)
                {
                    head.Add(new Text(string.Join("\n", lines.Take(lineCount)), text.Style));
                    var tailContents = new List<IContent>
                    {
                        new Text(string.Join("\n", lines.Skip(lineCount)), text.Style),
                    };
                    tailContents.AddRange(section.Contents.Skip(index + 1));
                    var tail = new Section(
                        tailContents,
                        section.Layout with { SpaceBefore = 0f });
                    var height = used + separator + lineCount * lineHeight;
                    return new SectionLayoutResult.Split(
                        new FlowFragment(section, head, height),
                        tail);
                }
            }

            if (head.Count == 0)
            {
                if (itemHeight > pageHeight)
                {
                    return new SectionLayoutResult.Impossible(new LayoutFailure(
                        LayoutFailureKind.SectionTooLarge,
                        $"Content in {section.GetType().Name} measures {itemHeight:0.##} points, larger than the page body.",
                        section.GetType(),
                        width,
                        pageHeight,
                        itemHeight));
                }

                return new SectionLayoutResult.NeedsNewPage();
            }

            var remainingContents = section.Contents.Skip(index).ToArray();
            var remainingSection = new Section(
                remainingContents,
                section.Layout with { SpaceBefore = 0f });
            return new SectionLayoutResult.Split(
                new FlowFragment(section, head, used),
                remainingSection);
        }

        return new SectionLayoutResult.Split(
            new FlowFragment(section, head, used),
            new Section([], section.Layout with { SpaceBefore = 0f }));
    }

    static SectionLayoutResult LayoutColumns(
        Document document,
        ColumnsSection section,
        float width,
        float availableHeight,
        float pageHeight,
        ITextMeasurer measurer,
        float before)
    {
        var widths = ResolveWidths(
            section.Columns.Select(static column => column.Width).ToArray(),
            section.Columns.Select(static column => column.Contents).ToArray(),
            width,
            section.GapPt,
            document,
            measurer);
        if (widths is null)
        {
            return new SectionLayoutResult.Impossible(new LayoutFailure(
                LayoutFailureKind.InvalidColumnConfiguration,
                $"Column widths for {section.GetType().Name} cannot be resolved.",
                section.GetType(),
                width,
                availableHeight));
        }

        var height = 0f;
        for (var i = 0; i < section.Columns.Count; i++)
            height = System.Math.Max(height, MeasureContents(document, section.Columns[i].Contents, widths[i], measurer));
        height += before;
        if (height <= availableHeight)
            return new SectionLayoutResult.Fits(new ColumnsFragment(section, height, widths));
        if (height <= pageHeight && section.Layout.KeepTogether)
            return new SectionLayoutResult.NeedsNewPage();
        return new SectionLayoutResult.Impossible(new LayoutFailure(
            LayoutFailureKind.SectionTooLarge,
            $"Columns section measures {height:0.##} points, larger than the usable page body.",
            section.GetType(),
            width,
            pageHeight,
            height));
    }

    static SectionLayoutResult LayoutBox(
        Document document,
        Box section,
        float width,
        float availableHeight,
        float pageHeight,
        ITextMeasurer measurer,
        float before)
    {
        var height = MeasureContents(document, section.Contents, width - 12f, measurer) + 12f + before;
        if (height <= availableHeight)
            return new SectionLayoutResult.Fits(new BoxFragment(section, height));
        if (height <= pageHeight && section.Layout.KeepTogether)
            return new SectionLayoutResult.NeedsNewPage();
        return new SectionLayoutResult.Impossible(new LayoutFailure(
            LayoutFailureKind.SectionTooLarge,
            $"Box measures {height:0.##} points, larger than the usable page body.",
            section.GetType(),
            width,
            pageHeight,
            height));
    }

    static SectionLayoutResult LayoutTable(
        Document document,
        TableSection table,
        float width,
        float availableHeight,
        float pageHeight,
        ITextMeasurer measurer,
        bool continuation,
        float before)
    {
        var columns = table.Columns;
        if (columns.Count == 0)
            return new SectionLayoutResult.Fits(new TableFragment(table, [], null, before, continuation, false));

        var columnWidths = ResolveWidths(
            columns.Select(static column => column.Width).ToArray(),
            TableContentsByColumn(table),
            width,
            0f,
            document,
            measurer,
            table.Options.CellPaddingPt * 2f);
        if (columnWidths is null)
        {
            return new SectionLayoutResult.Impossible(new LayoutFailure(
                LayoutFailureKind.InvalidColumnConfiguration,
                $"Table widths for {table.GetType().Name} cannot be resolved.",
                table.GetType(),
                width,
                availableHeight));
        }

        var header = table.Options.ShowHeader ? table.Options.Header : null;
        var headerHeight = header is null
            ? 0f
            : MeasureRow(document, header, columnWidths, table.Options.CellPaddingPt, measurer);
        var footerHeight = table.Options.Footer is { } footer
            ? MeasureRow(document, footer, columnWidths, table.Options.CellPaddingPt, measurer)
            : 0f;
        var completeHeight = before
            + headerHeight
            + table.Rows.Sum(row => MeasureRow(
                document,
                row,
                columnWidths,
                table.Options.CellPaddingPt,
                measurer))
            + footerHeight;
        if (table.Layout.KeepTogether
            && completeHeight > availableHeight
            && completeHeight <= pageHeight)
            return new SectionLayoutResult.NeedsNewPage();

        var firstDataIndex = 0;
        var rows = table.Rows;
        var used = before;
        var fragmentRows = new List<TableRow>();
        var hasHeader = header is not null;
        var repeated = continuation && table.Options.RepeatHeader;
        if (hasHeader)
            used += headerHeight;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowHeight = MeasureRow(
                document,
                rows[rowIndex],
                columnWidths,
                table.Options.CellPaddingPt,
                measurer);
            if (rowHeight > pageHeight)
            {
                return new SectionLayoutResult.Impossible(new LayoutFailure(
                    LayoutFailureKind.TableRowTooLarge,
                    $"Table row {rowIndex} measures {rowHeight:0.##} points, larger than the page body.",
                    table.GetType(),
                    width,
                    pageHeight,
                    rowHeight,
                    rowIndex));
            }

            if (used + rowHeight <= availableHeight)
            {
                fragmentRows.Add(rows[rowIndex]);
                used += rowHeight;
                firstDataIndex = rowIndex + 1;
                continue;
            }

            if (fragmentRows.Count == 0)
            {
                if (hasHeader && headerHeight + rowHeight > pageHeight)
                {
                    return new SectionLayoutResult.Impossible(new LayoutFailure(
                        LayoutFailureKind.SectionTooLarge,
                        $"Table header and row {rowIndex} cannot share an empty page.",
                        table.GetType(),
                        width,
                        pageHeight,
                        headerHeight + rowHeight,
                        rowIndex));
                }

                return new SectionLayoutResult.NeedsNewPage();
            }

            var tailRows = rows.Skip(firstDataIndex).ToArray();
            var tail = new TableSection(
                columns,
                tailRows,
                table.Options,
                table.Layout with { SpaceBefore = 0f });
            return new SectionLayoutResult.Split(
                new TableFragment(
                    table,
                    fragmentRows,
                    header,
                    used,
                    continuation,
                    repeated,
                    columnWidths),
                tail);
        }

        if (footerHeight > 0f && used + footerHeight <= availableHeight)
            used += footerHeight;

        return new SectionLayoutResult.Fits(
            new TableFragment(
                table,
                fragmentRows,
                header,
                used,
                continuation,
                repeated,
                columnWidths));
    }

    static float MeasureContents(
        Document document,
        IReadOnlyList<IContent> contents,
        float width,
        ITextMeasurer measurer)
    {
        var total = 0f;
        for (var i = 0; i < contents.Count; i++)
        {
            if (i > 0)
                total += ParagraphSpacing(document);
            total += MeasureContent(document, contents[i], width, measurer);
        }

        return total;
    }

    static float MeasureContent(
        Document document,
        IContent content,
        float width,
        ITextMeasurer measurer) =>
        content switch
        {
            Text text => measurer.MeasureHeight(
                text.Value,
                System.Math.Max(8f, width),
                TextStyleFor(document, text.Style)),
            Number number => measurer.MeasureHeight(
                FormatNumber(number),
                System.Math.Max(8f, width),
                TextStyleFor(document, null)),
            Heading heading => measurer.MeasureHeight(
                heading.Value,
                System.Math.Max(8f, width),
                HeadingStyle(document, heading)),
            Image image => image.Sizing.HeightPt,
            Spacer spacer => spacer.HeightPt,
            Rule => 1f,
            KeyValue pair => System.Math.Max(
                measurer.MeasureHeight(
                    pair.Key,
                    System.Math.Max(8f, width * 0.35f),
                    TextStyleFor(document, null)),
                MeasureContent(document, pair.Value, width * 0.65f, measurer)),
            Code code => MeasureCode(document, code, width, measurer),
            TableSection table => MeasureTableContent(document, table, width, measurer),
            ColumnsSection columns => MeasureColumnsContent(document, columns, width, measurer),
            IContainer container => MeasureContents(document, container.Contents, width, measurer),
            _ => throw new LayoutFailure(
                LayoutFailureKind.UnsupportedContent,
                $"No content layout exists for {content.GetType().FullName}.",
                content.GetType(),
                width),
        };

    static float MeasureTableContent(
        Document document,
        TableSection table,
        float width,
        ITextMeasurer measurer)
    {
        var columnWidths = ResolveWidths(
            table.Columns.Select(static column => column.Width).ToArray(),
            TableContentsByColumn(table),
            width,
            0f,
            document,
            measurer,
            table.Options.CellPaddingPt * 2f);
        if (columnWidths is null)
            throw new LayoutFailure(
                LayoutFailureKind.InvalidColumnConfiguration,
                $"Table widths for {table.GetType().Name} cannot be resolved.",
                table.GetType(),
                width);

        var height = table.Layout.SpaceBefore;
        if (table.Options.ShowHeader && table.Options.Header is { } header)
            height += MeasureRow(document, header, columnWidths, table.Options.CellPaddingPt, measurer);
        foreach (var row in table.Rows)
            height += MeasureRow(document, row, columnWidths, table.Options.CellPaddingPt, measurer);
        if (table.Options.Footer is { } footer)
            height += MeasureRow(document, footer, columnWidths, table.Options.CellPaddingPt, measurer);
        return height;
    }

    static float MeasureColumnsContent(
        Document document,
        ColumnsSection section,
        float width,
        ITextMeasurer measurer)
    {
        var widths = ResolveWidths(
            section.Columns.Select(static column => column.Width).ToArray(),
            section.Columns.Select(static column => column.Contents).ToArray(),
            width,
            section.GapPt,
            document,
            measurer);
        if (widths is null)
            throw new LayoutFailure(
                LayoutFailureKind.InvalidColumnConfiguration,
                $"Column widths for {section.GetType().Name} cannot be resolved.",
                section.GetType(),
                width);

        var height = 0f;
        for (var i = 0; i < section.Columns.Count; i++)
            height = System.Math.Max(
                height,
                MeasureContents(document, section.Columns[i].Contents, widths[i], measurer));
        return height;
    }

    static float MeasureCode(
        Document document,
        Code code,
        float width,
        ITextMeasurer measurer)
    {
        var style = new TextStyle(
            document.Typography.CodeFontFamily,
            System.Math.Max(6f, document.Typography.BodyFontSizePt - 2f),
            1.25f);
        return measurer.MeasureHeight(code.Source, System.Math.Max(8f, width - 12f), style) + 12f;
    }

    static float MeasureRow(
        Document document,
        TableRow row,
        IReadOnlyList<float> widths,
        float padding,
        ITextMeasurer measurer)
    {
        var height = 0f;
        for (var i = 0; i < widths.Count; i++)
        {
            var cell = i < row.Cells.Count ? row.Cells[i] : new TableCell(string.Empty);
            var cellHeight = MeasureContents(
                document,
                cell.Contents,
                System.Math.Max(8f, widths[i] - padding * 2f),
                measurer);
            height = System.Math.Max(height, cellHeight);
        }

        return System.Math.Max(
            document.Typography.EffectiveTableFontSizePt * document.Typography.LineHeight,
            height) + padding * 2f;
    }

    static float[]? ResolveWidths(
        IReadOnlyList<ColumnWidth> policies,
        IReadOnlyList<IReadOnlyList<IContent>> contents,
        float totalWidth,
        float gap,
        Document document,
        ITextMeasurer measurer,
        float autoPaddingPt = 0f)
    {
        if (policies.Count == 0 || totalWidth <= 0f)
            return policies.Count == 0 ? [] : null;
        var usable = totalWidth - gap * System.Math.Max(0, policies.Count - 1);
        if (usable <= 0f)
            return null;

        var widths = new float[policies.Count];
        var remaining = usable;
        var weights = 0f;
        var autoIndexes = new List<int>();
        for (var i = 0; i < policies.Count; i++)
        {
            switch (policies[i])
            {
                case ColumnWidth.Fixed fixedWidth when fixedWidth.Points >= 0f:
                    widths[i] = fixedWidth.Points;
                    remaining -= widths[i];
                    break;
                case ColumnWidth.Fixed:
                    return null;
                case ColumnWidth.Auto:
                    autoIndexes.Add(i);
                    break;
                case ColumnWidth.Weight weight when weight.Value > 0f:
                    weights += weight.Value;
                    break;
                default:
                    return null;
            }
        }

        if (remaining < 0f)
            return null;

        foreach (var index in autoIndexes)
        {
            var max = document.Typography.BodyFontSizePt;
            foreach (var content in contents.ElementAtOrDefault(index) ?? [])
                max = System.Math.Max(max, MeasureNaturalWidth(document, content, measurer));
            widths[index] = System.Math.Min(max + autoPaddingPt, remaining);
            remaining -= widths[index];
        }

        if (remaining < 0f)
            return null;

        if (weights > 0f)
        {
            for (var i = 0; i < policies.Count; i++)
            {
                if (policies[i] is ColumnWidth.Weight weight)
                    widths[i] = remaining * weight.Value / weights;
            }
        }
        else if (remaining > 0f)
        {
            var count = widths.Count(width => width <= 0f);
            if (count == 0)
                widths[^1] += remaining;
            else
            {
                var equal = remaining / count;
                for (var i = 0; i < widths.Length; i++)
                {
                    if (widths[i] <= 0f)
                        widths[i] = equal;
                }
            }
        }

        return widths.All(static width => width > 0f) ? widths : null;
    }

    static IReadOnlyList<IReadOnlyList<IContent>> TableContentsByColumn(TableSection table)
    {
        var result = new IReadOnlyList<IContent>[table.Columns.Count];
        for (var columnIndex = 0; columnIndex < result.Length; columnIndex++)
        {
            var contents = new List<IContent>();
            if (table.Options.Header is { } header
                && columnIndex < header.Cells.Count)
                contents.AddRange(header.Cells[columnIndex].Contents);
            foreach (var row in table.Rows)
            {
                if (columnIndex < row.Cells.Count)
                    contents.AddRange(row.Cells[columnIndex].Contents);
            }

            if (table.Options.Footer is { } footer
                && columnIndex < footer.Cells.Count)
                contents.AddRange(footer.Cells[columnIndex].Contents);
            result[columnIndex] = contents;
        }

        return result;
    }

    static float MeasureNaturalWidth(
        Document document,
        IContent content,
        ITextMeasurer measurer) =>
        content switch
        {
            Text text => measurer.MeasureWidth(
                text.Value,
                TextStyleFor(document, text.Style)),
            Number number => measurer.MeasureWidth(
                FormatNumber(number),
                TextStyleFor(document, null)),
            Heading heading => measurer.MeasureWidth(
                heading.Value,
                HeadingStyle(document, heading)),
            Image image => image.Sizing.WidthPt,
            Spacer => 0f,
            Rule => 0f,
            KeyValue pair => measurer.MeasureWidth(
                pair.Key,
                TextStyleFor(document, null))
                + MeasureNaturalWidth(document, pair.Value, measurer),
            Code code => measurer.MeasureWidth(
                code.Source,
                new TextStyle(
                    document.Typography.CodeFontFamily,
                    System.Math.Max(6f, document.Typography.BodyFontSizePt - 2f),
                    1.25f)),
            ColumnsSection columns => columns.Columns.Sum(column =>
                MeasureContentsNaturalWidth(document, column.Contents, measurer))
                + columns.GapPt * System.Math.Max(0, columns.Columns.Count - 1),
            IContainer container => MeasureContentsNaturalWidth(document, container.Contents, measurer),
            _ => document.Typography.BodyFontSizePt,
        };

    static float MeasureContentsNaturalWidth(
        Document document,
        IReadOnlyList<IContent> contents,
        ITextMeasurer measurer) =>
        contents.Count == 0
            ? document.Typography.BodyFontSizePt
            : contents.Max(content => MeasureNaturalWidth(document, content, measurer));

    static SectionLayout GetLayout(ISection section) => section switch
    {
        Section value => value.Layout,
        ColumnsSection value => value.Layout,
        TableSection value => value.Layout,
        Box value => value.Layout,
        _ => SectionLayout.Default,
    };

    static float SpaceAfter(ISection section) => GetLayout(section).SpaceAfter;

    static bool ShouldMoveWithNext(
        Document document,
        ISection section,
        ISection next,
        float width,
        float pageHeight,
        float usedHeight,
        ITextMeasurer measurer)
    {
        var current = MinimumSectionHeight(document, section, width, measurer)
            + System.Math.Max(0f, GetLayout(section).SpaceAfter);
        var following = MinimumSectionHeight(document, next, width, measurer);
        var required = current + following;
        var remaining = pageHeight - usedHeight;
        return required <= pageHeight && required > remaining;
    }

    static float MinimumSectionHeight(
        Document document,
        ISection section,
        float width,
        ITextMeasurer measurer)
    {
        var layout = GetLayout(section);
        var before = System.Math.Max(0f, layout.SpaceBefore);
        return section switch
        {
            PageBreakSection => 0f,
            Section flow when flow.Contents.Count == 0 => before,
            Section flow => before + MeasureContent(document, flow.Contents[0], width, measurer),
            ColumnsSection columns => before
                + MeasureColumnsContent(document, columns, width, measurer),
            TableSection table => before + MinimumTableHeight(document, table, width, measurer),
            Box box => before + MeasureContents(document, box.Contents, width - 12f, measurer) + 12f,
            _ => before,
        };
    }

    static float MinimumTableHeight(
        Document document,
        TableSection table,
        float width,
        ITextMeasurer measurer)
    {
        var columnWidths = ResolveWidths(
            table.Columns.Select(static column => column.Width).ToArray(),
            TableContentsByColumn(table),
            width,
            0f,
            document,
            measurer,
            table.Options.CellPaddingPt * 2f);
        if (columnWidths is null)
            throw new LayoutFailure(
                LayoutFailureKind.InvalidColumnConfiguration,
                $"Table widths for {table.GetType().Name} cannot be resolved.",
                table.GetType(),
                width);

        var height = 0f;
        if (table.Options.ShowHeader && table.Options.Header is { } header)
            height += MeasureRow(document, header, columnWidths, table.Options.CellPaddingPt, measurer);
        if (table.Rows.Count > 0)
            height += MeasureRow(document, table.Rows[0], columnWidths, table.Options.CellPaddingPt, measurer);
        return height;
    }

    static float ParagraphSpacing(Document document) => System.Math.Max(0f, document.Typography.ParagraphSpacingPt);

    static TextStyle TextStyleFor(Document document, ContentStyle? style) =>
        new(
            style?.FontFamily ?? document.Typography.BodyFontFamily,
            System.Math.Max(1f, style?.FontSizePt ?? document.Typography.BodyFontSizePt),
            document.Typography.LineHeight,
            style?.Bold ?? false);

    static TextStyle HeadingStyle(Document document, Heading heading) =>
        new(
            heading.Style?.FontFamily ?? document.Typography.BodyFontFamily,
            heading.Style?.FontSizePt ?? heading.Level switch
            {
                1 => document.Typography.H1SizePt,
                2 => document.Typography.H2SizePt,
                3 => document.Typography.H3SizePt,
                _ => document.Typography.H4SizePt,
            },
            document.Typography.LineHeight,
            true);

    static string FormatNumber(Number number)
    {
        var culture = CultureInfo.InvariantCulture;
        var format = number.Format;
        var decimals = format.DecimalPlaces;
        var pattern = decimals is { } count
            ? $"N{count}"
            : "N";
        var value = format.Style == NumberStyle.Percentage ? number.Value * 100m : number.Value;
        var result = value.ToString(pattern, culture);
        if (format.Style == NumberStyle.Percentage)
            result += " %";
        if (format.Style == NumberStyle.Currency && !string.IsNullOrWhiteSpace(format.CurrencyCode))
            result += $" {format.CurrencyCode}";
        return result;
    }

    static void Place(
        PageSlice page,
        SectionFragment fragment,
        float y,
        List<TocEntry> outline)
    {
        page.Fragments = [.. page.Fragments, new PlacedFragment(fragment, y, fragment.HeightPt)];
        if (fragment is TableFragment table)
        {
            var legacy = new TableBlock
            {
                Headers = table.Header is { } header
                    ? header.Cells.Select(CellText).ToArray()
                    : [],
                Rows = table.Rows
                    .Select(static row => (IReadOnlyList<string>)row.Cells.Select(CellText).ToArray())
                    .ToArray(),
                ShowHeader = table.Header is not null,
                RepeatHeaderOnPageBreak = table.Table.Options.RepeatHeader,
                HeaderBackground = table.Table.Options.HeaderBackground,
                RuleStyle = table.Table.Options.RuleStyle,
            };
            page.Blocks = [.. page.Blocks, new PlacedBlock(legacy, y, fragment.HeightPt)];
        }
        if (fragment is FlowFragment flow)
        {
            foreach (var heading in flow.Contents.OfType<Heading>().Where(static heading => heading.Level == 1))
            {
                outline.Add(new TocEntry(heading.Value, page.Number));
                page.ChapterTitle = heading.Value;
            }
        }
    }

    static string CellText(TableCell cell) => string.Join(
        " ",
        cell.Contents.Select(static content => content switch
        {
            Text text => text.Value,
            Number number => number.Value.ToString(CultureInfo.InvariantCulture),
            Heading heading => heading.Value,
            _ => string.Empty,
        }).Where(static value => !string.IsNullOrEmpty(value)));

    static bool HeaderApplies(Header? header, PageKind kind) => header is not null && kind switch
    {
        PageKind.First => header.IncludeFirstPage,
        PageKind.Last => header.IncludeLastPage,
        PageKind.Toc => header.IncludeToc,
        _ => header.IncludeBody,
    };

    static bool FooterApplies(Footer? footer, PageKind kind) => footer is not null && kind switch
    {
        PageKind.First => footer.IncludeFirstPage,
        PageKind.Last => footer.IncludeLastPage,
        PageKind.Toc => footer.IncludeToc,
        _ => footer.IncludeBody,
    };

    static PageSlice CreatePage(PageKind kind, int number) => new()
    {
        Kind = kind,
        Number = number,
        ShowHeader = kind switch
        {
            PageKind.First => false,
            PageKind.Last => false,
            PageKind.Toc => false,
            _ => true,
        },
        ShowFooter = kind switch
        {
            PageKind.First => false,
            PageKind.Last => false,
            PageKind.Toc => false,
            _ => true,
        },
        Fragments = [],
        Blocks = [],
    };
}
