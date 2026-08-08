using Novolis.Documents;
using Novolis.Math.Measure;
using TUnit.Core;

namespace Novolis.Documents.Unit;

public sealed class DocumentBuilderTests
{
    [Test]
    public async Task Build_requires_title()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Document.Create().Build());
        await Assert.That(ex!.Message).Contains("Title");
    }

    [Test]
    public async Task P1_body_spine_assembles_first_content_last()
    {
        var doc = Document.Create("Harbor Notes")
            .Meta(m => m.Author("Novolis").Subtitle("Sample"))
            .Page(p => p
                .A4()
                .Bands(0f, 12f)
                .Header(h => h.Template("{title}").FontSize(8f).IncludeBody())
                .Footer(f => f.Template("{page}").FontSize(8f).IncludeBody()))
            .Typography(t => t.BodySize(9f).TableSize(8f).LineHeight(1.2f))
            .Body(b => b
                .Content(c => c
                    .H2("Invoice")
                    .Columns(cols => cols
                        .Gap(12f)
                        .Fractions(0.5f, 0.5f)
                        .Column(left => left.H3("Supplier").Paragraph("Acme"))
                        .Column(right => right.H3("Bill to").Paragraph("Buyer")))
                    .Table(t => t
                        .Headers("#", "Item", "Amount")
                        .Row("1", "Widget", "10,00")
                        .ColumnWidths(0.1f, 0.6f, 0.3f)
                        .Align(CellAlign.Left, CellAlign.Left, CellAlign.Right)
                        .Rules(TableRuleStyle.Horizontal)
                        .HeaderBackground())
                    .Paragraph("Thanks.")))
            .Build();

        await Assert.That(doc.Meta.Title).IsEqualTo("Harbor Notes");
        await Assert.That(doc.Meta.Author).IsEqualTo("Novolis");
        await Assert.That(doc.Setup.Trim.Width.Points).IsEqualTo(TrimPresets.A4.Width.Points);
        await Assert.That(doc.IncludeCover).IsFalse();
        await Assert.That(doc.IncludeToc).IsFalse();
        await Assert.That(doc.Header!.Template).IsEqualTo("{title}");
        await Assert.That(doc.Body.Count).IsEqualTo(4);
        await Assert.That(doc.Body[0]).IsTypeOf<HeadingBlock>();
        await Assert.That(doc.Body[1]).IsTypeOf<ColumnsBlock>();
        await Assert.That(doc.Body[2]).IsTypeOf<TableBlock>();

        var table = (TableBlock)doc.Body[2];
        await Assert.That(table.Headers.Count).IsEqualTo(3);
        await Assert.That(table.ColumnAlignments![2]).IsEqualTo(CellAlign.Right);
    }

    [Test]
    public async Task Chapter_header_footer_includes_and_watermark_use_named_red()
    {
        var doc = Document.Create("Book")
            .Meta(m => m
                .Author("Novolis")
                .Publisher("Novolis-Platform")
                .Subject("Documents")
                .Keywords("pdf", "layout")
                .Identifier("DOC-1")
                .Language("en")
                .Version("1.0")
                .Date(new DateOnly(2026, 8, 8))
                .Description("Sample"))
            .Page(p => p
                .Trade6x9()
                .Header(h => h
                    .Template("{title}")
                    .IncludeBody()
                    .UseChapterTitle())
                .Footer(f => f
                    .Template("{page} / {pages}")
                    .IncludeFirstPage()
                    .IncludeToc()
                    .IncludeBody()
                    .IncludeLastPage()))
            .Watermark(w => w.Text("DRAFT").Color(DocumentColor.Red).Opacity(0.1f).On(WatermarkPages.All))
            .Body(b => b
                .First(f => f.Lines("Trade paperback", "Skia sample"))
                .Content(c => c
                    .Toc()
                    .Chapter("Chapter One", ch => ch
                        .Paragraph("Body.")
                        .H2("Aside")
                        .Paragraph("More.")))
                .Last(l => l
                    .Title("Colophon")
                    .Lines("End of sample.")
                    .Blocks(blocks => blocks.Table(t => t.Headers("K", "V").Row("Engine", "Skia")))))
            .Build();

        await Assert.That(doc.HasFirstPage).IsTrue();
        await Assert.That(doc.Meta.Publisher).IsEqualTo("Novolis-Platform");
        await Assert.That(doc.Watermark!.Color).IsEqualTo(DocumentColor.Red);
        await Assert.That(doc.Header!.UseChapterTitle).IsTrue();
        await Assert.That(doc.Footer!.IncludeFirstPage).IsTrue();
        await Assert.That(doc.Footer.IncludeToc).IsTrue();
        await Assert.That(doc.Footer.IncludeLastPage).IsTrue();
        await Assert.That(doc.IncludeToc).IsTrue();

        await Assert.That(doc.Body[0]).IsTypeOf<HeadingBlock>();
        var h1 = (HeadingBlock)doc.Body[0];
        await Assert.That(h1.Level).IsEqualTo(1);
        await Assert.That(h1.Text).IsEqualTo("Chapter One");
    }
}
