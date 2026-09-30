using System.Text;
using Novolis.Documents;
using Novolis.Documents.Layout;
using Novolis.Documents.Skia;
using TUnit.Core;

namespace Novolis.Documents.Unit;

public sealed class SkiaPdfTests
{
    [Test]
    public async Task ToBytes_inch6x9_has_cover_sections_and_bytes()
    {
        var document = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Sample", Author = "Tester" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            IncludeCover = true,
            IncludeToc = true,
            Footer = new Footer
            {
                Template = "{page}",
                IncludeFirstPage = true,
                IncludeToc = true,
                IncludeBody = true,
            },
            Header = new Header { Template = "{title}", IncludeBody = true },
            Body =
            [
                new HeadingBlock { Level = 1, Text = "Section One" },
                new ParagraphBlock { Text = "The river ran cold through the valley." },
                new HeadingBlock { Level = 1, Text = "Section Two" },
                new ParagraphBlock { Text = "Morning light found the bridge empty." },
            ],
        };

        var bytes = DocumentPdf.ToBytes(document);
        await Assert.That(bytes.Length).IsGreaterThan(500);
        await Assert.That(bytes.Length).IsLessThan(80_000);
        await Assert.That(bytes[0]).IsEqualTo((byte)'%');
        await Assert.That(bytes[1]).IsEqualTo((byte)'P');

        var pdf = Encoding.Latin1.GetString(bytes);
        await Assert.That(pdf).Contains("/Type /Outlines");
        await Assert.That(pdf).Contains("/Prev");
        await Assert.That(pdf).Contains(Utf16Title("Section One"));
        await Assert.That(pdf).Contains(Utf16Title("Section Two"));
        await Assert.That(pdf).DoesNotContain(Utf16Title("Contents"));
        await Assert.That(pdf).Contains("/Dest [");

        var laidOut = DocumentPdf.Layout(document);
        await Assert.That(laidOut.Pages.Any(p => p.Kind == PageKind.Toc)).IsFalse();
        var model = Novolis.Documents.Layout.PdfDocument.From(document.Meta, laidOut);
        await Assert.That(model.Outlines.Count).IsEqualTo(2);
        await Assert.That(model.Outlines[0].Title).IsEqualTo("Section One");
        await Assert.That(model.Outlines[1].Title).IsEqualTo("Section Two");
        await Assert.That(model.Outlines[1].PageNumber).IsGreaterThan(model.Outlines[0].PageNumber);

        var outDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".novolis", "artifacts", "toc-outline");
        Directory.CreateDirectory(outDir);
        await File.WriteAllBytesAsync(Path.Combine(outDir, "sample.pdf"), bytes);
    }

    [Test]
    public async Task ToBytes_long_table_writes_multi_page_pdf()
    {
        var rows = Enumerable.Range(1, 70)
            .Select(i => (IReadOnlyList<string>)[$"{i}", $"Line item {i}", $"{i * 12.5:0.00}"])
            .ToArray();

        var document = Document.Create("Page-broken table")
            .Page(p => p
                .A4()
                .Footer(f => f.Template("{page} / {pages}").IncludeBody()))
            .Body(b => b
                .Content(c => c
                    .Chapter("Manifest", ch => ch
                        .Paragraph("Long table should continue onto following pages with a repeated header.")
                        .Table(t => t
                            .Headers("#", "Description", "Amount")
                            .Rows(rows)
                            .ColumnWidths(0.1f, 0.7f, 0.2f)
                            .Align(CellAlign.Left, CellAlign.Left, CellAlign.Right)
                            .Rules(TableRuleStyle.Horizontal)
                            .HeaderBackground()
                            .RepeatHeaderOnPageBreak()))))
            .Build();

        var plan = DocumentPdf.Layout(document);
        var tableSlices = plan.Pages.Count(p => p.Blocks.Any(b => b.Block is TableBlock));
        await Assert.That(tableSlices).IsGreaterThanOrEqualTo(2);

        var outDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".novolis", "artifacts", "page-broken-table");
        Directory.CreateDirectory(outDir);
        var path = Path.Combine(outDir, "page-broken-table.pdf");
        DocumentPdf.Write(document, path);

        var bytes = await File.ReadAllBytesAsync(path);
        await Assert.That(bytes.Length).IsGreaterThan(800);
        await Assert.That(bytes[0]).IsEqualTo((byte)'%');
    }

    [Test]
    public async Task ToBytes_code_block_with_line_numbers_and_spans()
    {
        var document = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Code" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography { CodeFontFamily = "Consolas" },
            IncludeCover = false,
            IncludeToc = false,
            Body =
            [
                new CodeBlock
                {
                    ShowLineNumbers = true,
                    FirstLineNumber = 1,
                    AccentBorderLeftPt = 3f,
                    AccentColor = DocumentColor.Parse("#4a90e2"),
                    Background = DocumentColor.Parse("#f8f8f8"),
                    BorderStrokePt = 0.6f,
                    StyledLines =
                    [
                        new CodeLine
                        {
                            Spans =
                            [
                                new CodeSpan("public ", DocumentColor.Parse("#0550ae")),
                                new CodeSpan("void ", DocumentColor.Parse("#0550ae")),
                                new CodeSpan("Run()", DocumentColor.Parse("#1a1a1a")),
                            ],
                        },
                        CodeLine.FromPlain("{"),
                        new CodeLine
                        {
                            Spans =
                            [
                                new CodeSpan("  "),
                                new CodeSpan("Console", DocumentColor.Parse("#267f99")),
                                new CodeSpan(".WriteLine("),
                                new CodeSpan("\"hi\"", DocumentColor.Parse("#a31515")),
                                new CodeSpan(");"),
                            ],
                        },
                        CodeLine.FromPlain("}"),
                    ],
                },
            ],
        };

        var plan = DocumentPdf.Layout(document);
        await Assert.That(plan.Pages.Count).IsEqualTo(1);
        var placed = plan.Pages[0].Blocks.Single().Block as CodeBlock;
        await Assert.That(placed).IsNotNull();
        await Assert.That(placed!.ShowLineNumbers).IsTrue();
        await Assert.That(placed.StyledLines!.Count).IsEqualTo(4);

        var bytes = DocumentPdf.ToBytes(document);
        await Assert.That(bytes.Length).IsGreaterThan(400);
        await Assert.That(bytes[0]).IsEqualTo((byte)'%');
        await Assert.That(Encoding.Latin1.GetString(bytes)).DoesNotContain("/Outlines");
    }

    static string Utf16Title(string text)
    {
        var encoded = Encoding.BigEndianUnicode.GetBytes(text);
        var hex = new StringBuilder(encoded.Length * 2 + 6);
        hex.Append("<FEFF");
        foreach (var b in encoded)
            hex.Append(b.ToString("X2"));
        hex.Append('>');
        return hex.ToString();
    }
}
