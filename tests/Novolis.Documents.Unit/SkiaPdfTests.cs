using Novolis.Documents;
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
}
