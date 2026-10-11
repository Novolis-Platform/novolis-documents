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
        await Assert.That(fragments.Skip(1).All(fragment =>
                fragment.ColumnWidths.SequenceEqual(fragments[0].ColumnWidths)))
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

    [Test]
    public async Task Table_resolves_fixed_auto_and_weighted_widths_once()
    {
        var table = new TableSection(
            [
                new TableColumn(ColumnWidth.FixedPoints(72f)),
                new TableColumn(ColumnWidth.AutoSize()),
                new TableColumn(ColumnWidth.WeightBy(1f)),
            ],
            [new TableRow("1", "Automatic description", "Amount")],
            new TableOptions
            {
                Header = new TableRow("No.", "Description", "Total"),
            });
        var document = new Document
        {
            Meta = new DocumentMeta { Title = "Widths" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            Sections = [table],
        };

        var fragment = DocumentPaginator.Paginate(document, new FakeTextMeasurer())
            .Pages
            .SelectMany(static page => page.Fragments)
            .Select(static placed => placed.Fragment)
            .OfType<TableFragment>()
            .Single();

        await Assert.That(fragment.ColumnWidths.Count).IsEqualTo(3);
        await Assert.That(fragment.ColumnWidths[0]).IsEqualTo(72f);
        await Assert.That(fragment.ColumnWidths[1]).IsGreaterThan(0f);
        await Assert.That(fragment.ColumnWidths[2]).IsGreaterThan(0f);
    }

    [Test]
    public async Task Table_can_disable_header_repetition_without_losing_rows()
    {
        var rows = Enumerable.Range(1, 140)
            .Select(static number => new TableRow(number.ToString(), new string('x', 80)))
            .ToArray();
        var document = new Document
        {
            Meta = new DocumentMeta { Title = "No repeat" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            Sections =
            [
                new TableSection(
                    [
                        new TableColumn(ColumnWidth.WeightBy(1f)),
                        new TableColumn(ColumnWidth.WeightBy(3f)),
                    ],
                    rows,
                    new TableOptions
                    {
                        Header = new TableRow("No.", "Description"),
                        RepeatHeader = false,
                    }),
            ],
        };

        var fragments = DocumentPaginator.Paginate(document, new FakeTextMeasurer())
            .Pages
            .SelectMany(static page => page.Fragments)
            .Select(static placed => placed.Fragment)
            .OfType<TableFragment>()
            .ToArray();

        await Assert.That(fragments.Length).IsGreaterThan(1);
        await Assert.That(fragments[0].Header).IsNotNull();
        await Assert.That(fragments.Skip(1).All(static fragment => fragment.Header is null))
            .IsTrue();
        await Assert.That(fragments.SelectMany(static fragment => fragment.Rows).Count)
            .IsEqualTo(rows.Length);
    }
}
