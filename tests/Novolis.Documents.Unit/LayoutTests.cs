using Novolis.Documents;
using Novolis.Documents.Layout;
using TUnit.Core;

namespace Novolis.Documents.Unit;

/// <summary>Deterministic measurer: ~0.5em width per char, line height from style.</summary>
file sealed class FakeTextMeasurer : ITextMeasurer
{
    public float MeasureHeight(string text, float widthPt, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return style.FontSizePt * style.LineHeight;

        var avgChar = style.FontSizePt * 0.5f;
        var charsPerLine = System.Math.Max(1, (int)(widthPt / avgChar));
        var lines = 0;
        foreach (var para in text.Replace("\r\n", "\n").Split('\n'))
        {
            var len = System.Math.Max(1, para.Length);
            lines += (len + charsPerLine - 1) / charsPerLine;
        }

        return System.Math.Max(style.FontSizePt, lines * style.FontSizePt * style.LineHeight);
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
        Header = new RunningChrome { Template = "{title}" },
        Footer = new RunningChrome { Template = "{page}" },
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
    public async Task Paginate_cover_suppresses_chrome_and_body_has_footer()
    {
        var plan = DocumentPaginator.Paginate(SampleDocument(toc: false), new FakeTextMeasurer());
        await Assert.That(plan.Pages.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(plan.Pages[0].Kind).IsEqualTo(PageKind.Cover);
        await Assert.That(plan.Pages[0].ShowHeader).IsFalse();
        await Assert.That(plan.Pages[0].ShowFooter).IsFalse();

        var body = plan.Pages.Where(p => p.Kind == PageKind.Body).ToList();
        await Assert.That(body.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(body[0].ShowFooter).IsTrue();
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
    public async Task Paginate_toc_lists_h1_with_page_numbers()
    {
        var plan = DocumentPaginator.Paginate(SampleDocument(toc: true), new FakeTextMeasurer());
        await Assert.That(plan.Pages.Any(p => p.Kind == PageKind.Toc)).IsTrue();
        await Assert.That(plan.TocEntries.Count).IsEqualTo(2);
        await Assert.That(plan.TocEntries.All(e => e.PageNumber > 0)).IsTrue();
    }
}
