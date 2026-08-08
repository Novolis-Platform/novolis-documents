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
    public async Task Paginate_cover_defaults_to_footer_only_and_body_has_footer()
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
}
