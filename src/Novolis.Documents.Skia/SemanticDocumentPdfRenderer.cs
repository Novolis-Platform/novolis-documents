using System.Globalization;
using System.Text;
using Novolis.Documents;
using Novolis.Documents.Layout;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Skia painting pipeline for the semantic section page plan.</summary>
internal static class SemanticDocumentPdfRenderer
{
    const string RegularFontResource = "Novolis.Documents.Skia.Fonts.LiberationSerif-Regular.ttf";
    const string BoldFontResource = "Novolis.Documents.Skia.Fonts.LiberationSerif-Bold.ttf";

    public static byte[] ToBytes(Document document, DocumentPdfOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new DocumentPdfOptions();

        using var typeface = LoadTypeface(options.BodyFontPath, bold: false);
        using var boldTypeface = LoadTypeface(options.BoldFontPath ?? options.BodyFontPath, bold: true);
        using var monoTypeface = LoadMonoTypeface(document.Typography.CodeFontFamily);
        var measurer = new SkiaTextMeasurer(
            typeface,
            boldTypeface,
            monoTypeface,
            document.Typography.CodeFontFamily);
        var plan = SectionPaginator.Paginate(document, measurer);
        var pdfDocument = options.Outlines is { Count: > 0 } custom
            ? PdfDocument.From(document.Meta, plan, custom)
            : PdfDocument.From(document.Meta, plan);

        using var stream = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(stream))
        {
            ArgumentNullException.ThrowIfNull(pdf);
            var width = document.Setup.Trim.Width.Points;
            var height = document.Setup.Trim.Height.Points;
            foreach (var page in plan.Pages)
            {
                using var canvas = pdf.BeginPage(width, height);
                DrawPage(
                    canvas,
                    document,
                    page,
                    plan.Pages.Count,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer);
                pdf.EndPage();
            }

            pdf.Close();
        }

        return PdfOutlineWriter.Append(stream.ToArray(), pdfDocument);
    }

    static void DrawPage(
        SKCanvas canvas,
        Document document,
        PageSlice page,
        int pageCount,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer)
    {
        canvas.Clear(SKColors.White);
        var margin = document.Setup.Margin;
        var contentX = margin.Left.Points;
        var contentTop = margin.Top.Points + document.Setup.HeaderBand.Points;
        var contentWidth = document.Setup.Trim.Width.Points - margin.Horizontal.Points;
        var pageWidth = document.Setup.Trim.Width.Points;
        var pageHeight = document.Setup.Trim.Height.Points;

        DrawWatermark(canvas, document, page, pageWidth, pageHeight, boldTypeface);
        foreach (var placed in page.Fragments)
        {
            var y = contentTop + placed.YInContentPt;
            DrawFragment(
                canvas,
                document,
                placed.Fragment,
                typeface,
                boldTypeface,
                monoTypeface,
                measurer,
                contentX,
                y,
                contentWidth);
        }

        if (page.ShowHeader && document.Header is { } header)
        {
            var text = FormatTemplate(
                header.Template,
                document,
                page.Number,
                pageCount,
                page.ChapterTitle);
            DrawCenteredText(
                canvas,
                text,
                typeface,
                header.FontSizePt,
                contentX,
                margin.Top.Points,
                contentWidth,
                document.Setup.HeaderBand.Points);
        }

        if (page.ShowFooter && document.Footer is { } footer)
        {
            var text = FormatTemplate(
                footer.Template,
                document,
                page.Number,
                pageCount,
                page.ChapterTitle);
            var footerY = pageHeight - margin.Bottom.Points - document.Setup.FooterBand.Points;
            DrawCenteredText(
                canvas,
                text,
                typeface,
                footer.FontSizePt,
                contentX,
                footerY,
                contentWidth,
                document.Setup.FooterBand.Points);
        }
    }

    static void DrawFragment(
        SKCanvas canvas,
        Document document,
        SectionFragment fragment,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        switch (fragment)
        {
            case FlowFragment flow:
                DrawFlow(
                    canvas,
                    document,
                    flow,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer,
                    x,
                    y,
                    width);
                break;
            case ColumnsFragment columns:
                DrawColumns(
                    canvas,
                    document,
                    (ColumnsSection)columns.Source,
                    columns.ColumnWidths,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer,
                    x,
                    y + ((ColumnsSection)columns.Source).Layout.SpaceBefore,
                    width);
                break;
            case TableFragment table:
                DrawTable(
                    canvas,
                    document,
                    table,
                    typeface,
                    boldTypeface,
                    measurer,
                    x,
                    y,
                    width);
                break;
            case BoxFragment box:
                DrawBox(
                    canvas,
                    document,
                    (Box)box.Source,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer,
                    x,
                    y + ((Box)box.Source).Layout.SpaceBefore,
                    width);
                break;
        }
    }

    static void DrawFlow(
        SKCanvas canvas,
        Document document,
        FlowFragment fragment,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        var section = fragment.Source as Section;
        var cy = y + (section?.Layout.SpaceBefore ?? 0f);
        foreach (var content in fragment.Contents)
        {
            DrawContent(
                canvas,
                document,
                content,
                typeface,
                boldTypeface,
                monoTypeface,
                measurer,
                x,
                cy,
                width);
            cy += MeasureContent(document, content, width, measurer);
            cy += document.Typography.ParagraphSpacingPt;
        }
    }

    static void DrawContent(
        SKCanvas canvas,
        Document document,
        IContent content,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        switch (content)
        {
            case Text text:
                DrawWrappedText(
                    canvas,
                    text.Value,
                    ResolveTypeface(typeface, boldTypeface, text.Style?.Bold ?? false),
                    text.Style?.FontSizePt ?? document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight,
                    x,
                    y,
                    width,
                    text.Style?.Color ?? document.Theme.TextColor);
                break;
            case Number number:
                DrawWrappedText(
                    canvas,
                    FormatNumber(number),
                    typeface,
                    document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight,
                    x,
                    y,
                    width,
                    document.Theme.TextColor);
                break;
            case Heading heading:
                DrawWrappedText(
                    canvas,
                    heading.Value,
                    boldTypeface,
                    HeadingSize(document, heading),
                    document.Typography.LineHeight,
                    x,
                    y,
                    width,
                    heading.Style?.Color ?? document.Theme.TextColor);
                break;
            case Image image:
                DrawImage(canvas, image, x, y);
                break;
            case Rule rule:
                using (var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = ToColor(rule.Color),
                    StrokeWidth = rule.StrokeWidthPt,
                })
                {
                    canvas.DrawLine(x, y + 0.5f, x + width, y + 0.5f, paint);
                }

                break;
            case TableSection table:
                DrawTable(
                    canvas,
                    document,
                    new TableFragment(
                        table,
                        table.Rows,
                        table.Options.ShowHeader ? table.Options.Header : null,
                        MeasureTable(document, table, width, measurer),
                        isContinuation: false,
                        hasRepeatedHeader: false,
                        columnWidths: ResolveWidths(table, width, document, measurer)),
                    typeface,
                    boldTypeface,
                    measurer,
                    x,
                    y,
                    width);
                break;
            case Spacer:
                break;
            case KeyValue pair:
                DrawWrappedText(
                    canvas,
                    pair.Key,
                    boldTypeface,
                    document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight,
                    x,
                    y,
                    width * 0.35f,
                    document.Theme.TextColor);
                DrawContent(
                    canvas,
                    document,
                    pair.Value,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer,
                    x + width * 0.35f,
                    y,
                    width * 0.65f);
                break;
            case Code code:
                DrawCode(
                    canvas,
                    document,
                    code,
                    monoTypeface,
                    x,
                    y,
                    width);
                break;
            case ColumnsSection columns:
                DrawColumns(
                    canvas,
                    document,
                    columns,
                    null,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer,
                    x,
                    y,
                    width);
                break;
            case IContainer container:
                var cy = y;
                foreach (var nested in container.Contents)
                {
                    DrawContent(
                        canvas,
                        document,
                        nested,
                        typeface,
                        boldTypeface,
                        monoTypeface,
                        measurer,
                        x,
                        cy,
                        width);
                    cy += MeasureContent(document, nested, width, measurer)
                        + document.Typography.ParagraphSpacingPt;
                }

                break;
        }
    }

    static void DrawColumns(
        SKCanvas canvas,
        Document document,
        ColumnsSection columns,
        IReadOnlyList<float>? resolvedWidths,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        var widths = resolvedWidths is { Count: > 0 }
            ? resolvedWidths
            : ResolveWidths(columns, width, document, measurer);
        if (widths is null)
            return;

        var cx = x;
        for (var i = 0; i < columns.Columns.Count; i++)
        {
            var cy = y;
            foreach (var content in columns.Columns[i].Contents)
            {
                DrawContent(
                    canvas,
                    document,
                    content,
                    typeface,
                    boldTypeface,
                    monoTypeface,
                    measurer,
                    cx,
                    cy,
                    widths[i]);
                cy += MeasureContent(document, content, widths[i], measurer)
                    + document.Typography.ParagraphSpacingPt;
            }

            cx += widths[i] + columns.GapPt;
        }
    }

    static void DrawTable(
        SKCanvas canvas,
        Document document,
        TableFragment fragment,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        var table = fragment.Table;
        var widths = fragment.ColumnWidths.Count == table.Columns.Count
            ? fragment.ColumnWidths.ToArray()
            : ResolveWidths(table, width, document, measurer);
        if (widths is null)
            return;

        var padding = table.Options.CellPaddingPt;
        var yy = y + table.Layout.SpaceBefore;
        using var rule = new SKPaint
        {
            IsAntialias = true,
            Color = ToColor(document.Theme.TableRuleColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = document.Typography.TableRuleStrokePt,
        };
        using var headerFill = new SKPaint
        {
            IsAntialias = true,
            Color = ToColor(document.Theme.TableHeaderBackground),
            Style = SKPaintStyle.Fill,
        };
        using var bodyFont = new SKFont(
            typeface,
            document.Typography.EffectiveTableFontSizePt);
        using var headerFont = new SKFont(
            boldTypeface,
            document.Typography.EffectiveTableFontSizePt);

        if (fragment.Header is { } header)
            DrawRow(header, headerFont, header: true);
        foreach (var row in fragment.Rows)
            DrawRow(row, bodyFont, header: false);

        void DrawRow(TableRow row, SKFont font, bool header)
        {
            var rowHeight = MeasureRow(document, row, widths, padding, measurer);
            if (header && table.Options.HeaderBackground)
                canvas.DrawRect(x, yy, width, rowHeight, headerFill);

            var cellX = x;
            for (var i = 0; i < widths.Length; i++)
            {
                var cell = i < row.Cells.Count ? row.Cells[i] : new TableCell(string.Empty);
                if (table.Options.RuleStyle == TableRuleStyle.Grid)
                    canvas.DrawRect(cellX, yy, widths[i], rowHeight, rule);
                var text = string.Join(
                    " ",
                    cell.Contents.Select(static content => content switch
                    {
                        Text value => value.Value,
                        Number value => FormatNumber(value),
                        Heading value => value.Value,
                        _ => string.Empty,
                    }).Where(static value => !string.IsNullOrEmpty(value)));
                var lines = measurer.WrapLines(
                    text,
                    System.Math.Max(8f, widths[i] - padding * 2f),
                    new TextStyle(
                        document.Typography.BodyFontFamily,
                        document.Typography.EffectiveTableFontSizePt,
                        document.Typography.LineHeight,
                        header));
                var align = table.Columns[i].Alignment;
                var textAlign = align switch
                {
                    HorizontalAlignment.Center => SKTextAlign.Center,
                    HorizontalAlignment.Right => SKTextAlign.Right,
                    _ => SKTextAlign.Left,
                };
                var tx = align switch
                {
                    HorizontalAlignment.Center => cellX + widths[i] / 2f,
                    HorizontalAlignment.Right => cellX + widths[i] - padding,
                    _ => cellX + padding,
                };
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = ToColor(document.Theme.TextColor),
                };
                var ty = yy + padding + font.Size;
                foreach (var line in lines)
                {
                    canvas.DrawText(line, tx, ty, textAlign, font, paint);
                    ty += font.Size * document.Typography.LineHeight;
                }

                cellX += widths[i];
            }

            yy += rowHeight;
            if (table.Options.RuleStyle == TableRuleStyle.Horizontal)
                canvas.DrawLine(x, yy, x + width, yy, rule);
        }
    }

    static void DrawBox(
        SKCanvas canvas,
        Document document,
        Box box,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        var height = System.Math.Max(0f, MeasureContents(document, box.Contents, width - 12f, measurer) + 12f);
        if (box.Background is { } background)
        {
            using var fill = new SKPaint
            {
                IsAntialias = true,
                Color = ToColor(background),
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRect(x, y, width, height, fill);
        }

        using (var border = new SKPaint
        {
            IsAntialias = true,
            Color = ToColor(box.BorderColor),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.7f,
        })
        {
            canvas.DrawRect(x, y, width, height, border);
        }

        var cy = y + 6f;
        foreach (var content in box.Contents)
        {
            DrawContent(
                canvas,
                document,
                content,
                typeface,
                boldTypeface,
                monoTypeface,
                measurer,
                x + 6f,
                cy,
                width - 12f);
            cy += MeasureContent(document, content, width - 12f, measurer)
                + document.Typography.ParagraphSpacingPt;
        }
    }

    static float MeasureContents(
        Document document,
        IReadOnlyList<IContent> contents,
        float width,
        SkiaTextMeasurer measurer)
    {
        var height = 0f;
        for (var i = 0; i < contents.Count; i++)
        {
            if (i > 0)
                height += document.Typography.ParagraphSpacingPt;
            height += MeasureContent(document, contents[i], width, measurer);
        }

        return height;
    }

    static float MeasureContent(
        Document document,
        IContent content,
        float width,
        SkiaTextMeasurer measurer) =>
        content switch
        {
            Text text => measurer.MeasureHeight(
                text.Value,
                System.Math.Max(8f, width),
                new TextStyle(
                    text.Style?.FontFamily ?? document.Typography.BodyFontFamily,
                    text.Style?.FontSizePt ?? document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight,
                    text.Style?.Bold ?? false)),
            Number number => measurer.MeasureHeight(
                FormatNumber(number),
                System.Math.Max(8f, width),
                new TextStyle(
                    document.Typography.BodyFontFamily,
                    document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight)),
            Heading heading => measurer.MeasureHeight(
                heading.Value,
                System.Math.Max(8f, width),
                new TextStyle(
                    document.Typography.BodyFontFamily,
                    HeadingSize(document, heading),
                    document.Typography.LineHeight,
                    true)),
            Image image => image.Sizing.HeightPt,
            Spacer spacer => spacer.HeightPt,
            Rule => 1f,
            KeyValue pair => System.Math.Max(
                measurer.MeasureHeight(
                    pair.Key,
                    System.Math.Max(8f, width * 0.35f),
                    new TextStyle(
                        document.Typography.BodyFontFamily,
                        document.Typography.BodyFontSizePt,
                        document.Typography.LineHeight,
                        true)),
                MeasureContent(document, pair.Value, width * 0.65f, measurer)),
            Code code => measurer.MeasureHeight(
                code.Source,
                System.Math.Max(8f, width - 12f),
                new TextStyle(
                    document.Typography.CodeFontFamily,
                    System.Math.Max(6f, document.Typography.BodyFontSizePt - 2f),
                    1.25f)) + 12f,
            TableSection table => MeasureTable(document, table, width, measurer),
            ColumnsSection columns => MeasureColumnsContent(document, columns, width, measurer),
            IContainer container => MeasureContents(document, container.Contents, width, measurer),
            _ => document.Typography.BodyFontSizePt * document.Typography.LineHeight,
        };

    static float MeasureTable(
        Document document,
        TableSection table,
        float width,
        SkiaTextMeasurer measurer)
    {
        var widths = ResolveWidths(table, width, document, measurer);
        if (widths is null)
            return 0f;

        var height = 0f;
        if (table.Options.ShowHeader && table.Options.Header is { } header)
            height += MeasureRow(document, header, widths, table.Options.CellPaddingPt, measurer);
        foreach (var row in table.Rows)
            height += MeasureRow(document, row, widths, table.Options.CellPaddingPt, measurer);
        if (table.Options.Footer is { } footer)
            height += MeasureRow(document, footer, widths, table.Options.CellPaddingPt, measurer);
        return height + table.Layout.SpaceBefore;
    }

    static float MeasureColumnsContent(
        Document document,
        ColumnsSection section,
        float width,
        SkiaTextMeasurer measurer)
    {
        var widths = ResolveWidths(section, width, document, measurer);
        if (widths is null)
            return 0f;
        var height = 0f;
        for (var i = 0; i < section.Columns.Count; i++)
            height = System.Math.Max(
                height,
                MeasureContents(document, section.Columns[i].Contents, widths[i], measurer));
        return height;
    }

    static float MeasureRow(
        Document document,
        TableRow row,
        IReadOnlyList<float> widths,
        float padding,
        SkiaTextMeasurer measurer)
    {
        var height = 0f;
        for (var i = 0; i < widths.Count; i++)
        {
            var cell = i < row.Cells.Count ? row.Cells[i] : new TableCell(string.Empty);
            height = System.Math.Max(
                height,
                MeasureContents(
                    document,
                    cell.Contents,
                    System.Math.Max(8f, widths[i] - padding * 2f),
                    measurer));
        }

        return System.Math.Max(
            document.Typography.EffectiveTableFontSizePt * document.Typography.LineHeight,
            height) + padding * 2f;
    }

    static float[]? ResolveWidths(
        ColumnsSection columns,
        float width,
        Document document,
        SkiaTextMeasurer measurer)
    {
        var policies = columns.Columns.Select(static column => column.Width).ToArray();
        var contents = columns.Columns
            .Select(static column => column.Contents)
            .ToArray();
        return ResolveWidths(policies, contents, width, columns.GapPt, document, measurer);
    }

    static float[]? ResolveWidths(
        TableSection table,
        float width,
        Document document,
        SkiaTextMeasurer measurer)
    {
        var contents = TableContentsByColumn(table);
        return ResolveWidths(
            table.Columns.Select(static column => column.Width).ToArray(),
            contents,
            width,
            0f,
            document,
            measurer,
            table.Options.CellPaddingPt * 2f);
    }

    static float[]? ResolveWidths(
        IReadOnlyList<ColumnWidth> policies,
        IReadOnlyList<IReadOnlyList<IContent>> contents,
        float totalWidth,
        float gap,
        Document document,
        SkiaTextMeasurer measurer,
        float autoPaddingPt = 0f)
    {
        if (policies.Count == 0)
            return [];
        var usable = totalWidth - gap * System.Math.Max(0, policies.Count - 1);
        if (usable <= 0f)
            return null;
        var widths = new float[policies.Count];
        var remaining = usable;
        var weights = 0f;
        var autos = new List<int>();
        var weightedColumns = 0;
        for (var i = 0; i < policies.Count; i++)
        {
            switch (policies[i])
            {
                case ColumnWidth.Fixed fixedWidth when fixedWidth.Points > 0f:
                    widths[i] = fixedWidth.Points;
                    remaining -= widths[i];
                    break;
                case ColumnWidth.Fixed:
                    return null;
                case ColumnWidth.Auto:
                    autos.Add(i);
                    break;
                case ColumnWidth.Weight weight when weight.Value > 0f:
                    weights += weight.Value;
                    weightedColumns++;
                    break;
                default:
                    return null;
            }
        }

        if (remaining < 0f)
            return null;
        if (autos.Count > 0)
        {
            var autoBudget = weights > 0f
                ? remaining - weightedColumns * 0.01f
                : remaining;
            if (autoBudget <= 0f)
                return null;

            var desired = new float[autos.Count];
            var desiredTotal = 0f;
            for (var autoIndex = 0; autoIndex < autos.Count; autoIndex++)
            {
                var policyIndex = autos[autoIndex];
                var natural = document.Typography.BodyFontSizePt;
                foreach (var item in contents.ElementAtOrDefault(policyIndex) ?? [])
                    natural = System.Math.Max(
                        natural,
                        MeasureNaturalWidth(document, item, measurer));
                desired[autoIndex] = natural + autoPaddingPt;
                desiredTotal += desired[autoIndex];
            }

            var scale = desiredTotal > autoBudget
                ? autoBudget / desiredTotal
                : 1f;
            for (var autoIndex = 0; autoIndex < autos.Count; autoIndex++)
            {
                var policyIndex = autos[autoIndex];
                widths[policyIndex] = System.Math.Max(0.01f, desired[autoIndex] * scale);
                remaining -= widths[policyIndex];
            }
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
        else
        {
            var empty = widths.Count(static value => value <= 0f);
            if (empty > 0)
            {
                var each = remaining / empty;
                for (var i = 0; i < widths.Length; i++)
                {
                    if (widths[i] <= 0f)
                        widths[i] = each;
                }
            }
        }

        return widths.All(static value => value > 0f) ? widths : null;
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
        SkiaTextMeasurer measurer) =>
        content switch
        {
            Text text => measurer.MeasureWidth(
                text.Value,
                new TextStyle(
                    text.Style?.FontFamily ?? document.Typography.BodyFontFamily,
                    text.Style?.FontSizePt ?? document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight,
                    text.Style?.Bold ?? false)),
            Number number => measurer.MeasureWidth(
                FormatNumber(number),
                new TextStyle(
                    document.Typography.BodyFontFamily,
                    document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight)),
            Heading heading => measurer.MeasureWidth(
                heading.Value,
                new TextStyle(
                    document.Typography.BodyFontFamily,
                    HeadingSize(document, heading),
                    document.Typography.LineHeight,
                    true)),
            Image image => image.Sizing.WidthPt,
            Spacer => 0f,
            Rule => 0f,
            KeyValue pair => measurer.MeasureWidth(
                pair.Key,
                new TextStyle(
                    document.Typography.BodyFontFamily,
                    document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight,
                    true))
                + MeasureNaturalWidth(document, pair.Value, measurer),
            Code code => measurer.MeasureWidth(
                code.Source,
                new TextStyle(
                    document.Typography.CodeFontFamily,
                    System.Math.Max(6f, document.Typography.BodyFontSizePt - 2f),
                    1.25f)),
            IContainer container => container.Contents.Count == 0
                ? document.Typography.BodyFontSizePt
                : container.Contents.Max(
                    item => MeasureNaturalWidth(document, item, measurer)),
            _ => document.Typography.BodyFontSizePt,
        };

    static void DrawCode(
        SKCanvas canvas,
        Document document,
        Code code,
        SKTypeface monoTypeface,
        float x,
        float y,
        float width)
    {
        var fontSize = System.Math.Max(6f, document.Typography.BodyFontSizePt - 2f);
        var lineHeight = fontSize * 1.25f;
        var lines = code.Source.Replace("\r\n", "\n").Split('\n');
        var height = lines.Length * lineHeight + 12f;
        using var fill = new SKPaint
        {
            IsAntialias = true,
            Color = ToColor(DocumentColor.LightGray),
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawRect(x, y, width, height, fill);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ToColor(document.Theme.TextColor),
        };
        using var font = new SKFont(monoTypeface, fontSize);
        var ty = y + 6f + fontSize;
        foreach (var line in lines)
        {
            canvas.DrawText(line, x + 6f, ty, SKTextAlign.Left, font, paint);
            ty += lineHeight;
        }
    }

    static void DrawImage(SKCanvas canvas, Image image, float x, float y)
    {
        byte[]? bytes = image.Source switch
        {
            PathImageSource pathSource when File.Exists(pathSource.Path) => File.ReadAllBytes(pathSource.Path),
            RasterImageSource raster => raster.Data,
            SvgImageSource svg => svg.Data,
            _ => null,
        };
        if (bytes is null || bytes.Length == 0)
            return;

        var isSvg = image.Source is SvgImageSource
            || image.Source is PathImageSource svgPath
                && svgPath.Path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        if (isSvg)
        {
            DrawSvg(canvas, bytes, x, y, image.Sizing.WidthPt, image.Sizing.HeightPt);
            return;
        }

        using var data = SKData.CreateCopy(bytes);
        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null)
            return;
        canvas.DrawBitmap(
            bitmap,
            new SKRect(
                x,
                y,
                x + image.Sizing.WidthPt,
                y + image.Sizing.HeightPt),
            new SKSamplingOptions(SKFilterMode.Linear));
    }

    static void DrawSvg(SKCanvas canvas, byte[] bytes, float x, float y, float width, float height)
    {
        using var svg = new Svg.Skia.SKSvg();
        using var stream = new MemoryStream(bytes);
        if (svg.Load(stream) is null || svg.Picture is null)
            return;
        var bounds = svg.Picture.CullRect;
        if (bounds.Width <= 0f || bounds.Height <= 0f)
            return;
        var scale = System.Math.Min(width / bounds.Width, height / bounds.Height);
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(svg.Picture);
        canvas.Restore();
    }

    static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float fontSize,
        float lineHeight,
        float x,
        float y,
        float width,
        DocumentColor color)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ToColor(color),
        };
        using var font = new SKFont(typeface, fontSize);
        var lines = DocumentPdf.WrapLines(text, font, width);
        var ty = y + fontSize;
        foreach (var line in lines)
        {
            canvas.DrawText(line, x, ty, SKTextAlign.Left, font, paint);
            ty += fontSize * lineHeight;
        }
    }

    static void DrawCenteredText(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float fontSize,
        float x,
        float y,
        float width,
        float bandHeight)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
        };
        using var font = new SKFont(typeface, fontSize);
        canvas.DrawText(
            text,
            x + width / 2f,
            y + bandHeight * 0.7f,
            SKTextAlign.Center,
            font,
            paint);
    }

    static void DrawWatermark(
        SKCanvas canvas,
        Document document,
        PageSlice page,
        float pageWidth,
        float pageHeight,
        SKTypeface boldTypeface)
    {
        if (document.Watermark is not { Text: { Length: > 0 } text } watermark)
            return;
        var applies = page.Kind switch
        {
            PageKind.First => (watermark.Pages & WatermarkPages.First) != 0,
            PageKind.Last => (watermark.Pages & WatermarkPages.Last) != 0,
            _ => (watermark.Pages & WatermarkPages.Body) != 0,
        };
        if (!applies)
            return;
        var alpha = (byte)System.Math.Clamp((int)(System.Math.Clamp(watermark.Opacity, 0f, 1f) * 255f), 0, 255);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(
                watermark.Color.R,
                watermark.Color.G,
                watermark.Color.B,
                alpha),
        };
        using var font = new SKFont(boldTypeface, System.Math.Max(8f, watermark.FontSizePt));
        canvas.Save();
        canvas.Translate(pageWidth / 2f, pageHeight / 2f);
        canvas.RotateDegrees(watermark.RotationDegrees);
        canvas.DrawText(text, 0, 0, SKTextAlign.Center, font, paint);
        canvas.Restore();
    }

    static string FormatTemplate(
        string template,
        Document document,
        int pageNumber,
        int pageCount,
        string? chapterTitle)
    {
        var meta = document.Meta;
        var date = meta.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        var keywords = meta.Keywords.Count == 0 ? string.Empty : string.Join(", ", meta.Keywords);
        return template
            .Replace("{page}", pageNumber.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{pages}", pageCount.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{chapter}", chapterTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{section}", chapterTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{title}", meta.Title, StringComparison.OrdinalIgnoreCase)
            .Replace("{subtitle}", meta.Subtitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{author}", meta.Author ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{contributors}", meta.Contributors ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{series}", meta.Series ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{publisher}", meta.Publisher ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{subject}", meta.Subject ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{description}", meta.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{keywords}", keywords, StringComparison.OrdinalIgnoreCase)
            .Replace("{identifier}", meta.Identifier ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{language}", meta.Language ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", meta.Version ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", date, StringComparison.OrdinalIgnoreCase)
            .Replace("{rights}", meta.Rights ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    static float HeadingSize(Document document, Heading heading) => heading.Style?.FontSizePt ?? heading.Level switch
    {
        1 => document.Typography.H1SizePt,
        2 => document.Typography.H2SizePt,
        3 => document.Typography.H3SizePt,
        _ => document.Typography.H4SizePt,
    };

    static SKTypeface ResolveTypeface(SKTypeface regular, SKTypeface bold, bool isBold) =>
        isBold ? bold : regular;

    static SKColor ToColor(DocumentColor color) => new(color.R, color.G, color.B);

    static string FormatNumber(Number number)
    {
        var value = number.Format.Style == NumberStyle.Percentage
            ? number.Value * 100m
            : number.Value;
        var pattern = number.Format.DecimalPlaces is { } places
            ? $"N{places}"
            : "N";
        var result = value.ToString(pattern, CultureInfo.InvariantCulture);
        if (number.Format.Style == NumberStyle.Percentage)
            result += " %";
        if (number.Format.Style == NumberStyle.Currency
            && !string.IsNullOrWhiteSpace(number.Format.CurrencyCode))
            result += $" {number.Format.CurrencyCode}";
        return result;
    }

    static SKTypeface LoadTypeface(string? path, bool bold)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            var typeface = SKTypeface.FromFile(path);
            if (typeface is not null)
                return typeface;
        }

        using var stream = typeof(SemanticDocumentPdfRenderer).Assembly
            .GetManifestResourceStream(bold ? BoldFontResource : RegularFontResource);
        if (stream is not null)
        {
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var embedded = SKTypeface.FromData(SKData.CreateCopy(memory.ToArray()));
            if (embedded is not null)
                return embedded;
        }

        return SKTypeface.FromFamilyName(
                   "Georgia",
                   bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                   SKFontStyleWidth.Normal,
                   SKFontStyleSlant.Upright)
               ?? SKTypeface.Default;
    }

    static SKTypeface LoadMonoTypeface(string? family)
    {
        foreach (var name in new[]
                 {
                     string.IsNullOrWhiteSpace(family) ? "Consolas" : family.Trim(),
                     "Consolas",
                     "Courier New",
                     "Liberation Mono",
                     "Courier",
                 })
        {
            var typeface = SKTypeface.FromFamilyName(
                name,
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
            if (typeface is not null)
                return typeface;
        }

        return SKTypeface.Default;
    }
}
