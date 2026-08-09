using Novolis.Documents;
using Novolis.Documents.Layout;
using Novolis.Math.Measure;
using SkiaSharp;

namespace Novolis.Documents.Skia;

/// <summary>Options for PDF generation.</summary>
public sealed class DocumentPdfOptions
{
    /// <summary>Optional path to a body TrueType/OpenType font file. Overrides the embedded Liberation Serif subset.</summary>
    public string? BodyFontPath { get; init; }

    /// <summary>Optional path to a bold font file. Overrides the embedded Liberation Serif Bold subset.</summary>
    public string? BoldFontPath { get; init; }
}

/// <summary>Writes a <see cref="PagedDocument"/> to PDF via SkiaSharp.</summary>
public static class DocumentPdf
{
    const string RegularFontResource = "Novolis.Documents.Skia.Fonts.LiberationSerif-Regular.ttf";
    const string BoldFontResource = "Novolis.Documents.Skia.Fonts.LiberationSerif-Bold.ttf";

    /// <summary>Writes a PDF file.</summary>
    public static void Write(PagedDocument document, string path, DocumentPdfOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = ToBytes(document, options);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Generates PDF bytes.</summary>
    public static byte[] ToBytes(PagedDocument document, DocumentPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new DocumentPdfOptions();

        using var typeface = LoadTypeface(options.BodyFontPath, bold: false);
        using var boldTypeface = LoadTypeface(options.BoldFontPath ?? options.BodyFontPath, bold: true);
        using var monoTypeface = LoadMonoTypeface(document.Typography.CodeFontFamily);
        var measurer = new SkiaTextMeasurer(
            typeface, boldTypeface, monoTypeface, document.Typography.CodeFontFamily);
        var plan = DocumentPaginator.Paginate(document, measurer);

        using var stream = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(stream))
        {
            ArgumentNullException.ThrowIfNull(pdf);
            var width = document.Setup.Trim.Width.Points;
            var height = document.Setup.Trim.Height.Points;

            var pageCount = plan.Pages.Count;
            foreach (var page in plan.Pages)
            {
                using var canvas = pdf.BeginPage(width, height);
                DrawPage(canvas, document, page, pageCount, typeface, boldTypeface, monoTypeface, measurer);
                pdf.EndPage();
            }

            pdf.Close();
        }

        return stream.ToArray();
    }

    /// <summary>Paginates with the same measurer used for PDF output.</summary>
    public static PagePlan Layout(PagedDocument document, DocumentPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new DocumentPdfOptions();
        using var typeface = LoadTypeface(options.BodyFontPath, bold: false);
        using var boldTypeface = LoadTypeface(options.BoldFontPath ?? options.BodyFontPath, bold: true);
        using var monoTypeface = LoadMonoTypeface(document.Typography.CodeFontFamily);
        return DocumentPaginator.Paginate(
            document,
            new SkiaTextMeasurer(typeface, boldTypeface, monoTypeface, document.Typography.CodeFontFamily));
    }

    static void DrawPage(
        SKCanvas canvas,
        PagedDocument document,
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

        foreach (var placed in page.Blocks)
        {
            var y = contentTop + placed.YInContentPt;
            DrawBlock(canvas, document, placed.Block, typeface, boldTypeface, monoTypeface, measurer,
                contentX, y, contentWidth);
        }

        if (page.ShowHeader && document.Header is { } header)
        {
            var text = ResolveHeaderText(header, document, page, pageCount);
            DrawCenteredText(canvas, text, typeface, header.FontSizePt,
                contentX, margin.Top.Points, contentWidth, document.Setup.HeaderBand.Points);
        }

        if (page.ShowFooter && document.Footer is { } footer)
        {
            var text = FormatTemplate(footer.Template, document, page.Number, pageCount, page.ChapterTitle);
            var footerY = pageHeight - margin.Bottom.Points - document.Setup.FooterBand.Points;
            DrawCenteredText(canvas, text, typeface, footer.FontSizePt,
                contentX, footerY, contentWidth, document.Setup.FooterBand.Points);
        }
    }

    static string ResolveHeaderText(
        Header header,
        PagedDocument document,
        PageSlice page,
        int pageCount)
    {
        if (header.UseChapterTitle && !string.IsNullOrWhiteSpace(page.ChapterTitle))
            return page.ChapterTitle;

        return FormatTemplate(header.Template, document, page.Number, pageCount, page.ChapterTitle);
    }

    static void DrawWatermark(
        SKCanvas canvas,
        PagedDocument document,
        PageSlice page,
        float pageWidth,
        float pageHeight,
        SKTypeface boldTypeface)
    {
        if (document.Watermark is not { Text: { Length: > 0 } text } mark)
            return;
        if (!WatermarkApplies(mark.Pages, page.Kind))
            return;

        var opacity = System.Math.Clamp(mark.Opacity, 0f, 1f);
        if (opacity <= 0f)
            return;

        var alpha = (byte)System.Math.Clamp((int)(opacity * 255f), 0, 255);
        var ink = mark.Color;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(ink.R, ink.G, ink.B, alpha),
        };
        using var font = new SKFont(boldTypeface, System.Math.Max(8f, mark.FontSizePt));

        canvas.Save();
        canvas.Translate(pageWidth / 2f, pageHeight / 2f);
        canvas.RotateDegrees(mark.RotationDegrees);
        canvas.DrawText(text, 0, 0, SKTextAlign.Center, font, paint);
        canvas.Restore();
    }

    static bool WatermarkApplies(WatermarkPages pages, PageKind kind) => kind switch
    {
        PageKind.Cover => (pages & WatermarkPages.First) != 0,
        PageKind.Toc => (pages & WatermarkPages.Toc) != 0,
        PageKind.Last => (pages & WatermarkPages.Last) != 0,
        _ => (pages & WatermarkPages.Body) != 0,
    };

    static void DrawBlock(
        SKCanvas canvas,
        PagedDocument document,
        IBlock block,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        switch (block)
        {
            case HeadingBlock h:
                var hs = h.Level switch
                {
                    1 => document.Typography.H1SizePt,
                    2 => document.Typography.H2SizePt,
                    3 => document.Typography.H3SizePt,
                    _ => document.Typography.H4SizePt,
                };
                DrawWrappedText(canvas, h.Text, boldTypeface, hs, document.Typography.LineHeight, x, y, width);
                break;
            case ParagraphBlock p:
                DrawWrappedText(canvas, p.Text, typeface, document.Typography.BodyFontSizePt,
                    document.Typography.LineHeight, x, y, width);
                break;
            case TableBlock table:
                DrawTable(canvas, document, table, typeface, boldTypeface, x, y, width);
                break;
            case TextBoxBlock textBox:
                DrawTextBox(
                    canvas,
                    textBox,
                    textBox.UseMonospaceFont ? monoTypeface : typeface,
                    x,
                    y,
                    width);
                break;
            case CodeBlock code:
                DrawCode(canvas, code, monoTypeface, x, y, width);
                break;
            case ImageBlock image:
                DrawImage(canvas, image, x, y);
                break;
            case ColumnsBlock columns:
                DrawColumns(canvas, document, columns, typeface, boldTypeface, monoTypeface, measurer, x, y, width);
                break;
            case SceneBreakBlock s:
                DrawCenteredText(canvas, s.Ornament, typeface, document.Typography.SceneBreakSizePt,
                    x, y, width, document.Typography.SceneBreakSizePt * 1.2f);
                break;
            case LineBreakBlock or PageBreakBlock or BlankPageBlock or CoverBlock or TocBlock:
                break;
        }
    }

    static void DrawColumns(
        SKCanvas canvas,
        PagedDocument document,
        ColumnsBlock columns,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        SKTypeface monoTypeface,
        SkiaTextMeasurer measurer,
        float x,
        float y,
        float width)
    {
        if (columns.Columns.Count == 0)
            return;

        var gap = System.Math.Max(0f, columns.GapPt);
        var usable = System.Math.Max(0f, width - gap * (columns.Columns.Count - 1));
        var fractions = columns.Fractions;
        var colWidths = new float[columns.Columns.Count];
        if (fractions is null || fractions.Count != columns.Columns.Count)
        {
            var equal = usable / columns.Columns.Count;
            for (var i = 0; i < colWidths.Length; i++)
                colWidths[i] = equal;
        }
        else
        {
            float sum = 0f;
            for (var i = 0; i < fractions.Count; i++)
                sum += System.Math.Max(0f, fractions[i]);
            if (sum <= 0f)
                sum = 1f;
            for (var i = 0; i < colWidths.Length; i++)
                colWidths[i] = usable * System.Math.Max(0f, fractions[i]) / sum;
        }

        float cx = x;
        for (var i = 0; i < columns.Columns.Count; i++)
        {
            float cy = y;
            foreach (var child in columns.Columns[i])
            {
                DrawBlock(canvas, document, child, typeface, boldTypeface, monoTypeface, measurer, cx, cy, colWidths[i]);
                var h = child switch
                {
                    ImageBlock img => img.HeightPt,
                    HeadingBlock heading => measurer.MeasureHeight(heading.Text, colWidths[i],
                        new TextStyle(document.Typography.BodyFontFamily,
                            heading.Level switch
                            {
                                1 => document.Typography.H1SizePt,
                                2 => document.Typography.H2SizePt,
                                3 => document.Typography.H3SizePt,
                                _ => document.Typography.H4SizePt,
                            },
                            document.Typography.LineHeight,
                            Bold: true)),
                    ParagraphBlock p => measurer.MeasureHeight(p.Text, colWidths[i],
                        new TextStyle(document.Typography.BodyFontFamily, document.Typography.BodyFontSizePt,
                            document.Typography.LineHeight)),
                    TableBlock t => MeasureDrawnTable(document, t, colWidths[i], measurer),
                    _ => document.Typography.BodyFontSizePt,
                };
                cy += h + (child is HeadingBlock hb
                    ? (hb.Level == 1
                        ? document.Typography.AfterLevel1SpacingPt
                        : document.Typography.AfterHeadingSpacingPt)
                    : document.Typography.ParagraphSpacingPt);
            }

            cx += colWidths[i] + gap;
        }
    }

    static float MeasureDrawnTable(
        PagedDocument document,
        TableBlock table,
        float width,
        SkiaTextMeasurer measurer)
    {
        var columns = table.Headers.Count;
        foreach (var row in table.Rows)
            columns = System.Math.Max(columns, row.Count);
        if (columns <= 0)
            return 0f;

        var padding = document.Typography.TableCellPaddingPt;
        var style = new TextStyle(document.Typography.BodyFontFamily, document.Typography.EffectiveTableFontSizePt,
            document.Typography.LineHeight);
        var colWidths = ResolveColumnWidths(table.ColumnWidths, columns, width);
        float total = 0f;
        if (table.ShowHeader && table.Headers.Count > 0)
            total += MeasureDrawnRow(table.Headers, columns, colWidths, padding, style, measurer);
        foreach (var row in table.Rows)
            total += MeasureDrawnRow(row, columns, colWidths, padding, style, measurer);
        return total;
    }

    static float MeasureDrawnRow(
        IReadOnlyList<string> cells,
        int columns,
        float[] colWidths,
        float padding,
        TextStyle style,
        SkiaTextMeasurer measurer)
    {
        float max = style.FontSizePt * style.LineHeight;
        for (var c = 0; c < columns; c++)
        {
            var text = c < cells.Count ? cells[c] : string.Empty;
            var tw = System.Math.Max(8f, colWidths[c] - padding * 2f);
            max = System.Math.Max(max, measurer.MeasureHeight(text, tw, style));
        }

        return max + padding * 2f;
    }

    static void DrawImage(SKCanvas canvas, ImageBlock image, float x, float y)
    {
        var width = System.Math.Max(1f, image.WidthPt);
        var height = System.Math.Max(1f, image.HeightPt);
        byte[]? bytes = image.Data;
        string? path = image.Path;
        if (bytes is null && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            bytes = File.ReadAllBytes(path);
        if (bytes is null || bytes.Length == 0)
            return;

        var isSvg = LooksLikeSvg(bytes, path);
        if (isSvg)
        {
            DrawSvg(canvas, bytes, x, y, width, height);
            return;
        }

        using var data = SKData.CreateCopy(bytes);
        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null)
            return;
        var dest = new SKRect(x, y, x + width, y + height);
        canvas.DrawBitmap(bitmap, dest);
    }

    static bool LooksLikeSvg(byte[] bytes, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path)
            && path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return true;
        var probe = System.Text.Encoding.UTF8.GetString(bytes, 0, System.Math.Min(bytes.Length, 256));
        return probe.Contains("<svg", StringComparison.OrdinalIgnoreCase);
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

    static void DrawCode(
        SKCanvas canvas,
        CodeBlock code,
        SKTypeface monoTypeface,
        float x,
        float y,
        float width)
    {
        var pad = System.Math.Max(0f, code.PaddingPt);
        var fontSize = System.Math.Max(6f, code.FontSizePt);
        var lineStep = fontSize * System.Math.Max(1f, code.LineHeight);
        var lines = code.ResolveLines();
        var height = pad * 2f + System.Math.Max(1, lines.Count) * lineStep;

        if (code.Background is { } bg)
        {
            using var fill = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(bg.R, bg.G, bg.B),
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRect(x, y, width, height, fill);
        }

        if (code.BorderStrokePt > 0f)
        {
            var bc = code.BorderColor;
            using var border = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(bc.R, bc.G, bc.B),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = code.BorderStrokePt,
            };
            canvas.DrawRect(x, y, width, height, border);
        }

        if (code.AccentBorderLeftPt > 0f)
        {
            var ac = code.AccentColor;
            using var accent = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(ac.R, ac.G, ac.B),
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRect(x, y, code.AccentBorderLeftPt, height, accent);
        }

        using var font = new SKFont(monoTypeface, fontSize);
        var gutter = 0f;
        if (code.ShowLineNumbers)
        {
            var lastNum = code.FirstLineNumber + System.Math.Max(0, lines.Count - 1);
            var digits = System.Math.Max(2, lastNum.ToString().Length);
            gutter = font.MeasureText(new string('0', digits)) + 8f;
        }

        var textLeft = x + pad + gutter;
        canvas.Save();
        canvas.ClipRect(new SKRect(x, y, x + width, y + height));

        var ty = y + pad + fontSize;
        var lineNo = code.FirstLineNumber;
        foreach (var line in lines)
        {
            if (code.ShowLineNumbers)
            {
                var ln = code.LineNumberColor;
                using var lnPaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = new SKColor(ln.R, ln.G, ln.B),
                };
                var label = lineNo.ToString();
                var labelW = font.MeasureText(label);
                canvas.DrawText(label, textLeft - 6f - labelW, ty, SKTextAlign.Left, font, lnPaint);
            }

            var tx = textLeft;
            foreach (var span in line.Spans)
            {
                if (string.IsNullOrEmpty(span.Text))
                    continue;
                var ink = span.Color ?? code.TextColor;
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = new SKColor(ink.R, ink.G, ink.B),
                };
                canvas.DrawText(span.Text, tx, ty, SKTextAlign.Left, font, paint);
                tx += font.MeasureText(span.Text);
            }

            ty += lineStep;
            lineNo++;
        }

        canvas.Restore();
    }

    static void DrawTextBox(
        SKCanvas canvas,
        TextBoxBlock box,
        SKTypeface typeface,
        float x,
        float y,
        float width)
    {
        var pad = System.Math.Max(0f, box.PaddingPt);
        var fontSize = System.Math.Max(6f, box.FontSizePt);
        var lineStep = fontSize * System.Math.Max(1f, box.LineHeight);
        var gap = System.Math.Max(0f, box.LineGapPt);
        var lines = box.Lines.Count == 0 ? (IReadOnlyList<string>)[string.Empty] : box.Lines;
        var textWidth = System.Math.Max(8f, width - pad * 2f);

        using var font = new SKFont(typeface, fontSize);
        float contentH = 0f;
        var wrapped = new List<IReadOnlyList<string>>(lines.Count);
        foreach (var line in lines)
        {
            var parts = WrapLines(line, font, textWidth);
            wrapped.Add(parts);
            contentH += parts.Count * lineStep + gap;
        }

        var height = pad * 2f + contentH;

        if (box.Background is { } bg)
        {
            using var fill = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(bg.R, bg.G, bg.B),
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRect(x, y, width, height, fill);
        }

        if (box.BorderStrokePt > 0f)
        {
            var bc = box.BorderColor;
            using var border = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(bc.R, bc.G, bc.B),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = box.BorderStrokePt,
            };
            canvas.DrawRect(x, y, width, height, border);
        }

        if (box.AccentBorderLeftPt > 0f)
        {
            var ac = box.AccentColor;
            using var accent = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(ac.R, ac.G, ac.B),
                Style = SKPaintStyle.Fill,
            };
            canvas.DrawRect(x, y, box.AccentBorderLeftPt, height, accent);
        }

        var ink = box.TextColor;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(ink.R, ink.G, ink.B),
        };

        var ty = y + pad + fontSize;
        foreach (var parts in wrapped)
        {
            foreach (var part in parts)
            {
                canvas.DrawText(part, x + pad, ty, SKTextAlign.Left, font, paint);
                ty += lineStep;
            }

            ty += gap;
        }
    }

    static void DrawTable(
        SKCanvas canvas,
        PagedDocument document,
        TableBlock table,
        SKTypeface typeface,
        SKTypeface boldTypeface,
        float x,
        float y,
        float width)
    {
        var columns = table.Headers.Count;
        foreach (var row in table.Rows)
            columns = System.Math.Max(columns, row.Count);
        if (columns <= 0)
            return;

        var typography = document.Typography;
        var padding = typography.TableCellPaddingPt;
        var fontSize = typography.EffectiveTableFontSizePt;
        var lineHeight = typography.LineHeight;
        var colWidths = ResolveColumnWidths(table.ColumnWidths, columns, width);
        var aligns = table.ColumnAlignments;
        var yy = y;
        var tableBottom = y;

        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        using var rule = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0x33, 0x33, 0x33),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = typography.TableRuleStrokePt,
        };
        using var headerFill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xEF, 0xF1, 0xF4),
            Style = SKPaintStyle.Fill,
        };
        using var bodyFont = new SKFont(typeface, fontSize);
        using var headerFont = new SKFont(boldTypeface, fontSize);

        CellAlign AlignAt(int c) =>
            aligns is { Count: > 0 } && c < aligns.Count ? aligns[c] : CellAlign.Left;

        SKTextAlign SkAlign(CellAlign a) => a switch
        {
            CellAlign.Center => SKTextAlign.Center,
            CellAlign.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left,
        };

        float TextX(float cellX, float colW, CellAlign align) => align switch
        {
            CellAlign.Center => cellX + colW / 2f,
            CellAlign.Right => cellX + colW - padding,
            _ => cellX + padding,
        };

        void DrawRow(IReadOnlyList<string> cells, SKFont font, bool header)
        {
            float rowH = fontSize * lineHeight;
            for (var c = 0; c < columns; c++)
            {
                var text = c < cells.Count ? cells[c] : string.Empty;
                var textWidth = System.Math.Max(8f, colWidths[c] - padding * 2f);
                var lines = WrapLines(text, font, textWidth);
                rowH = System.Math.Max(rowH, lines.Count * fontSize * lineHeight);
            }

            rowH += padding * 2f;
            var cellY = yy;

            if (header && table.HeaderBackground)
                canvas.DrawRect(x, cellY, width, rowH, headerFill);

            if (table.RuleStyle == TableRuleStyle.Grid)
            {
                float cellX = x;
                for (var c = 0; c < columns; c++)
                {
                    canvas.DrawRect(cellX, cellY, colWidths[c], rowH, rule);
                    cellX += colWidths[c];
                }
            }

            float textCellX = x;
            for (var c = 0; c < columns; c++)
            {
                var text = c < cells.Count ? cells[c] : string.Empty;
                var colW = colWidths[c];
                var align = AlignAt(c);
                var textWidth = System.Math.Max(8f, colW - padding * 2f);
                var lines = WrapLines(text, font, textWidth);
                var ty = cellY + padding + fontSize;
                var tx = TextX(textCellX, colW, align);
                var skAlign = SkAlign(align);
                foreach (var line in lines)
                {
                    canvas.DrawText(line, tx, ty, skAlign, font, paint);
                    ty += fontSize * lineHeight;
                }

                textCellX += colW;
            }

            yy += rowH;
            tableBottom = yy;

            if (table.RuleStyle == TableRuleStyle.Horizontal)
                canvas.DrawLine(x, yy, x + width, yy, rule);
        }

        if (table.RuleStyle == TableRuleStyle.Horizontal)
            canvas.DrawLine(x, y, x + width, y, rule);

        if (table.ShowHeader && table.Headers.Count > 0)
            DrawRow(table.Headers, headerFont, header: true);

        foreach (var row in table.Rows)
            DrawRow(row, bodyFont, header: false);

        _ = tableBottom;
    }

    static float[] ResolveColumnWidths(IReadOnlyList<float>? fractions, int count, float totalWidth)
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

    static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float fontSize,
        float lineHeight,
        float x,
        float y,
        float width)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black,
        };
        using var font = new SKFont(typeface, fontSize);
        var lines = WrapLines(text, font, width);
        var step = fontSize * lineHeight;
        var yy = y + fontSize;
        foreach (var line in lines)
        {
            canvas.DrawText(line, x, yy, SKTextAlign.Left, font, paint);
            yy += step;
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
        var baseline = y + bandHeight * 0.7f;
        canvas.DrawText(text, x + width / 2f, baseline, SKTextAlign.Center, font, paint);
    }

    internal static List<string> WrapLines(string text, SKFont font, float maxWidth)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                result.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = string.Empty;
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(candidate) <= maxWidth || current.Length == 0)
                {
                    current = candidate;
                }
                else
                {
                    result.Add(current);
                    current = word;
                }
            }

            if (current.Length > 0)
                result.Add(current);
        }

        if (result.Count == 0)
            result.Add(string.Empty);
        return result;
    }

    static string FormatTemplate(
        string template,
        PagedDocument document,
        int pageNumber,
        int pageCount,
        string? chapterTitle)
    {
        var meta = document.Meta;
        var date = meta.Date?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        var keywords = meta.Keywords.Count == 0 ? string.Empty : string.Join(", ", meta.Keywords);
        return template
            .Replace("{page}", pageNumber.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{pages}", pageCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{chapter}", chapterTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{title}", meta.Title ?? string.Empty, StringComparison.OrdinalIgnoreCase)
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

    static SKTypeface LoadTypeface(string? path, bool bold)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            var tf = SKTypeface.FromFile(path);
            if (tf is not null)
                return tf;
        }

        var resource = bold ? BoldFontResource : RegularFontResource;
        var embedded = LoadEmbeddedTypeface(resource);
        if (embedded is not null)
            return embedded;

        return SKTypeface.FromFamilyName(
            "Georgia",
            bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright) ?? SKTypeface.Default;
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
            var tf = SKTypeface.FromFamilyName(
                name,
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
            if (tf is not null)
                return tf;
        }

        return SKTypeface.Default;
    }

    static SKTypeface? LoadEmbeddedTypeface(string resourceName)
    {
        var asm = typeof(DocumentPdf).Assembly;
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new MemoryStream();
        stream.CopyTo(reader);
        // CreateCopy so the typeface owns font bytes independent of this stream.
        return SKTypeface.FromData(SKData.CreateCopy(reader.ToArray()));
    }
}
