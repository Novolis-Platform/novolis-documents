using Novolis.Documents;
using Novolis.Documents.Layout;
using TUnit.Core;

namespace Novolis.Documents.Unit;

public sealed class SectionLayoutTests
{
    [Test]
    public async Task Semantic_document_uses_first_body_and_last_page_kinds()
    {
        var document = Document.Create("Semantic")
            .FirstPage(page => page.Section(section => section.Heading("Invoice", 1)))
            .Section(section => section.Text("Body"))
            .LastPage(page => page.Section(section => section.Text("Closing")))
            .Toc()
            .Build();

        var plan = DocumentPaginator.Paginate(document, new FakeTextMeasurer());

        await Assert.That(plan.Pages.Select(static page => page.Kind))
            .IsEquivalentTo([PageKind.First, PageKind.Body, PageKind.Last]);
        await Assert.That(plan.TocEntries).IsEquivalentTo(
            [new TocEntry("Invoice", 1)]);
        await Assert.That(plan.Pages.SelectMany(static page => page.Blocks)).IsEmpty();
        await Assert.That(plan.Pages.SelectMany(static page => page.Fragments)).IsNotEmpty();
    }

    [Test]
    public async Task Semantic_table_splits_between_rows_and_repeats_header()
    {
        var rows = Enumerable.Range(1, 160)
            .Select(static number => new TableRow(
                [
                    new TableCell(number.ToString()),
                    new TableCell(new string('x', 80)),
                ]))
            .ToArray();
        var document = new Document
        {
            Meta = new DocumentMeta { Title = "Table" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography { TableFontSizePt = 9f },
            Sections =
            [
                new TableSection(
                    [
                        new TableColumn(new ColumnWidth.Weight(1f)),
                        new TableColumn(new ColumnWidth.Weight(3f)),
                    ],
                    rows,
                    new TableOptions
                    {
                        Header = new TableRow("No.", "Description"),
                        RepeatHeader = true,
                    }),
            ],
        };

        var plan = DocumentPaginator.Paginate(document, new FakeTextMeasurer());
        var fragments = plan.Pages
            .SelectMany(static page => page.Fragments)
            .Select(static placed => placed.Fragment)
            .OfType<TableFragment>()
            .ToArray();

        await Assert.That(fragments.Length).IsGreaterThan(1);
        await Assert.That(fragments.Skip(1).All(static fragment => fragment.HasRepeatedHeader))
            .IsTrue();
        await Assert.That(fragments.SelectMany(static fragment => fragment.Rows).Count)
            .IsEqualTo(rows.Length);
        await Assert.That(fragments.SelectMany(static fragment => fragment.Rows)
                .Select(static row => row.Cells[0].Contents.OfType<Text>().Single().Value))
            .IsEquivalentTo(Enumerable.Range(1, rows.Length).Select(static value => value.ToString()));
    }

    [Test]
    public async Task Oversized_table_row_returns_typed_failure()
    {
        var document = new Document
        {
            Meta = new DocumentMeta { Title = "Oversized" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            Sections =
            [
                new TableSection(
                    [new TableColumn(new ColumnWidth.Weight(1f))],
                    [new TableRow([new TableCell(new string('x', 100_000))])],
                    new TableOptions
                    {
                        Header = new TableRow("Description"),
                    }),
            ],
        };

        var failure = Assert.Throws<LayoutFailure>(
            () => DocumentPaginator.Paginate(document, new FakeTextMeasurer()));

        await Assert.That(failure!.Kind).IsEqualTo(LayoutFailureKind.TableRowTooLarge);
        await Assert.That(failure.RowIndex).IsEqualTo(0);
    }
}
