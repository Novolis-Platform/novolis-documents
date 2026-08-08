using Novolis.Documents;
using Novolis.Documents.Layout;
using TUnit.Core;

namespace Novolis.Documents.Unit;

/// <summary>Deterministic measurer: ~0.5em width per char, line height from style.</summary>
file sealed class FakeTextMeasurer : ITextMeasurer
{
    public float MeasureHeight(string text, float widthPt, TextStyle style)
    {
        var lines = WrapLines(text, widthPt, style);
        return System.Math.Max(style.FontSizePt, lines.Count * style.FontSizePt * style.LineHeight);
    }

    public IReadOnlyList<string> WrapLines(string text, float widthPt, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return [string.Empty];

        var avgChar = style.FontSizePt * 0.5f;
        var charsPerLine = System.Math.Max(1, (int)(widthPt / avgChar));
        var result = new List<string>();
        foreach (var para in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (para.Length == 0)
            {
                result.Add(string.Empty);
                continue;
            }

            for (var i = 0; i < para.Length; i += charsPerLine)
                result.Add(para.Substring(i, System.Math.Min(charsPerLine, para.Length - i)));
        }

        return result.Count == 0 ? [string.Empty] : result;
    }
}

public sealed class LayoutTests
{
    static PagedDocument SampleDocument(bool toc = true) => new()
    {
        Meta = new DocumentMeta { Title = "Sample", Author = "Tester", Subtitle = "Demo" },
        Setup = new PageSetup
        {
            Trim = TrimPresets.Inch6x9,
            Margin = TrimPresets.DefaultMargin,
        },
        Typography = new Typography(),
        IncludeCover = true,
        IncludeToc = toc,
        Header = new Header { Template = "{title}", IncludeBody = true },
        Footer = new Footer
        {
            Template = "{page}",
            IncludeFirstPage = true,
            IncludeToc = true,
            IncludeBody = true,
            IncludeLastPage = true,
        },
        Body =
        [
            new HeadingBlock { Level = 1, Text = "Section One" },
            new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("word", 80)) },
            new SceneBreakBlock(),
            new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("more", 60)) },
            new HeadingBlock { Level = 1, Text = "Section Two" },
            new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("final", 40)) },
        ],
    };

    [Test]
    public async Task Paginate_header_and_footer_default_to_body_only()
    {
        var doc = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Defaults" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            IncludeCover = true,
            IncludeToc = true,
            Header = new Header { Template = "{title}" },
            Footer = new Footer { Template = "{page}" },
            Last = new LastPage { Title = "End", Lines = ["Done."] },
            Body =
            [
                new HeadingBlock { Level = 1, Text = "One" },
                new ParagraphBlock { Text = "Body." },
            ],
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        await Assert.That(plan.Pages[0].Kind).IsEqualTo(PageKind.Cover);
        await Assert.That(plan.Pages[0].ShowHeader).IsFalse();
        await Assert.That(plan.Pages[0].ShowFooter).IsFalse();
        await Assert.That(plan.Pages.Where(p => p.Kind == PageKind.Toc).All(p => !p.ShowHeader && !p.ShowFooter)).IsTrue();
        await Assert.That(plan.Pages.Where(p => p.Kind == PageKind.Body).All(p => p.ShowHeader && p.ShowFooter)).IsTrue();
        await Assert.That(plan.Pages.Where(p => p.Kind == PageKind.Last).All(p => !p.ShowHeader && !p.ShowFooter)).IsTrue();
    }

    [Test]
    public async Task Paginate_cover_can_opt_into_footer()
    {
        var plan = DocumentPaginator.Paginate(SampleDocument(toc: false), new FakeTextMeasurer());
        await Assert.That(plan.Pages.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(plan.Pages[0].Kind).IsEqualTo(PageKind.Cover);
        await Assert.That(plan.Pages[0].ShowHeader).IsFalse();
        await Assert.That(plan.Pages[0].ShowFooter).IsTrue();

        var body = plan.Pages.Where(p => p.Kind == PageKind.Body).ToList();
        await Assert.That(body.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(body[0].ShowFooter).IsTrue();
    }

    [Test]
    public async Task Paginate_footer_can_exclude_first_and_toc()
    {
        var doc = SampleDocument(toc: true);
        doc = new PagedDocument
        {
            Meta = doc.Meta,
            Setup = doc.Setup,
            Typography = doc.Typography,
            IncludeCover = true,
            IncludeToc = true,
            Header = doc.Header,
            Footer = new Footer
            {
                Template = "{page}",
                IncludeFirstPage = false,
                IncludeToc = false,
                IncludeBody = true,
                IncludeLastPage = false,
            },
            Body = doc.Body,
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        await Assert.That(plan.Pages[0].ShowFooter).IsFalse();
        await Assert.That(plan.Pages.Where(p => p.Kind == PageKind.Toc).All(p => !p.ShowFooter)).IsTrue();
    }

    [Test]
    public async Task Paginate_tracks_chapter_title_on_body_pages()
    {
        var doc = SampleDocument(toc: false);
        doc = new PagedDocument
        {
            Meta = doc.Meta,
            Setup = doc.Setup,
            Typography = doc.Typography,
            IncludeCover = false,
            IncludeToc = false,
            Header = new Header
            {
                Template = "{title}",
                IncludeBody = true,
                UseChapterTitle = true,
            },
            Footer = doc.Footer,
            Body = doc.Body,
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        var sectionTwo = plan.Pages.First(p =>
            p.Blocks.Any(b => b.Block is HeadingBlock { Level: 1, Text: "Section Two" }));
        await Assert.That(sectionTwo.ChapterTitle).IsEqualTo("Section Two");
    }

    [Test]
    public async Task Paginate_h1_starts_new_page_when_prior_content()
    {
        var plan = DocumentPaginator.Paginate(SampleDocument(toc: false), new FakeTextMeasurer());
        var h1Pages = plan.Pages
            .Where(p => p.Blocks.Any(b => b.Block is HeadingBlock { Level: 1, Text: "Section Two" }))
            .ToList();
        await Assert.That(h1Pages.Count).IsEqualTo(1);
        await Assert.That(h1Pages[0].Blocks[0].Block).IsAssignableTo<HeadingBlock>();
    }

    [Test]
    public async Task Paginate_first_and_last_overflow_onto_extra_pages()
    {
        var manyLines = Enumerable.Range(1, 80).Select(i => $"Line {i}").ToArray();
        var doc = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Overflow" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            IncludeCover = true,
            IncludeToc = false,
            First = new FirstPage { Lines = manyLines },
            Last = new LastPage { Title = "End", Lines = manyLines },
            Body = [new ParagraphBlock { Text = "Body." }],
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        await Assert.That(plan.Pages.Count(p => p.Kind == PageKind.Cover)).IsGreaterThan(1);
        await Assert.That(plan.Pages.Count(p => p.Kind == PageKind.Last)).IsGreaterThan(1);
    }

    [Test]
    public async Task Paginate_table_and_last_page()
    {
        var doc = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Table Doc" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            IncludeCover = false,
            IncludeToc = false,
            Last = new LastPage { Title = "End", Lines = ["Done."] },
            Body =
            [
                new HeadingBlock { Level = 1, Text = "Data" },
                new TableBlock
                {
                    Headers = ["A", "B"],
                    Rows = [["1", "2"], ["3", "4"]],
                },
            ],
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        await Assert.That(plan.Pages.Any(p => p.Blocks.Any(b => b.Block is TableBlock))).IsTrue();
        await Assert.That(plan.Pages.Any(p => p.Kind == PageKind.Last)).IsTrue();
    }

    [Test]
    public async Task Paginate_long_table_breaks_across_pages_and_repeats_header()
    {
        var rows = Enumerable.Range(1, 60)
            .Select(i => (IReadOnlyList<string>)[$"{i}", $"Cargo {i}", $"{i * 3}"])
            .ToArray();

        var doc = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Manifest" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography
            {
                TableFontSizePt = 10f,
                LineHeight = 1.2f,
                TableCellPaddingPt = 3f,
            },
            IncludeCover = false,
            IncludeToc = false,
            Body =
            [
                new HeadingBlock { Level = 1, Text = "Bonded lines" },
                new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("lead-in", 40)) },
                new TableBlock
                {
                    Headers = ["#", "Item", "Qty"],
                    Rows = rows,
                    RuleStyle = TableRuleStyle.Horizontal,
                    HeaderBackground = true,
                    RepeatHeaderOnPageBreak = true,
                },
            ],
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        var slices = plan.Pages
            .SelectMany(p => p.Blocks.Select(b => (p.Number, Table: b.Block as TableBlock)))
            .Where(x => x.Table is not null)
            .Select(x => (x.Number, Table: x.Table!))
            .ToList();

        await Assert.That(slices.Count).IsGreaterThanOrEqualTo(2);

        var totalRows = slices.Sum(s => s.Table.Rows.Count);
        await Assert.That(totalRows).IsEqualTo(60);

        await Assert.That(slices[0].Table.ShowHeader).IsTrue();
        await Assert.That(slices[0].Table.Headers).IsEquivalentTo(["#", "Item", "Qty"]);
        await Assert.That(slices.Skip(1).All(s => s.Table.ShowHeader)).IsTrue();
        await Assert.That(slices.Skip(1).All(s => s.Table.Headers.SequenceEqual(["#", "Item", "Qty"]))).IsTrue();

        // Row order preserved across breaks.
        var flattened = slices.SelectMany(s => s.Table.Rows.Select(r => r[0])).ToList();
        await Assert.That(flattened).IsEquivalentTo(Enumerable.Range(1, 60).Select(i => i.ToString()).ToList());
    }

    [Test]
    public async Task Paginate_table_keeps_header_on_first_slice_after_page_flush()
    {
        // Fill most of the first body page, then a table that must start on the next page.
        // RepeatHeaderOnPageBreak=false must still show the header on that first table page.
        var filler = string.Join(' ', Enumerable.Repeat("pad", 400));
        var rows = Enumerable.Range(1, 8)
            .Select(i => (IReadOnlyList<string>)[$"{i}", $"R{i}"])
            .ToArray();

        var doc = new PagedDocument
        {
            Meta = new DocumentMeta { Title = "Flush" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.Inch6x9,
                Margin = TrimPresets.DefaultMargin,
            },
            Typography = new Typography(),
            IncludeCover = false,
            IncludeToc = false,
            Body =
            [
                new ParagraphBlock { Text = filler },
                new TableBlock
                {
                    Headers = ["A", "B"],
                    Rows = rows,
                    RepeatHeaderOnPageBreak = false,
                },
            ],
        };

        var plan = DocumentPaginator.Paginate(doc, new FakeTextMeasurer());
        var firstTable = plan.Pages
            .SelectMany(p => p.Blocks)
            .Select(b => b.Block)
            .OfType<TableBlock>()
            .First();

        await Assert.That(firstTable.ShowHeader).IsTrue();
        await Assert.That(firstTable.Headers).IsEquivalentTo(["A", "B"]);
    }
}
