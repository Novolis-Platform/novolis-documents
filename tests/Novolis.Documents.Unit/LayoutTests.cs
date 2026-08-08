using Novolis.Documents;
using Novolis.Documents.Layout;
using TUnit.Core;

namespace Novolis.Documents.Unit;

/// <summary>Deterministic measurer: ~0.6em width per char, line height from style.</summary>
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
    static BookDocument SampleBook(bool toc = true) => new()
    {
        Meta = new BookMeta { Title = "Duckville", Author = "Tester", Subtitle = "A Tale" },
        Setup = new PageSetup
        {
            Trim = TrimPresets.TradePaperback6x9,
            Margin = TrimPresets.DefaultBookMargin,
        },
        Typography = new Typography(),
        IncludeCover = true,
        IncludeToc = toc,
        Header = new RunningChrome { Template = "{title}" },
        Footer = new RunningChrome { Template = "{page}" },
        Body =
        [
            new HeadingBlock { Level = 1, Text = "Chapter One" },
            new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("word", 80)) },
            new SceneBreakBlock(),
            new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("more", 60)) },
            new HeadingBlock { Level = 1, Text = "Chapter Two" },
            new ParagraphBlock { Text = string.Join(' ', Enumerable.Repeat("final", 40)) },
        ],
    };

    [Test]
    public async Task Paginate_cover_suppresses_chrome_and_body_has_footer()
    {
        var plan = BookPaginator.Paginate(SampleBook(toc: false), new FakeTextMeasurer());
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
        var plan = BookPaginator.Paginate(SampleBook(toc: false), new FakeTextMeasurer());
        var chapterPages = plan.Pages
            .Where(p => p.Blocks.Any(b => b.Block is HeadingBlock { Level: 1, Text: "Chapter Two" }))
            .ToList();
        await Assert.That(chapterPages.Count).IsEqualTo(1);
        await Assert.That(chapterPages[0].Blocks[0].Block).IsAssignableTo<HeadingBlock>();
    }

    [Test]
    public async Task Paginate_toc_lists_chapters_with_page_numbers()
    {
        var plan = BookPaginator.Paginate(SampleBook(toc: true), new FakeTextMeasurer());
        await Assert.That(plan.Pages.Any(p => p.Kind == PageKind.Toc)).IsTrue();
        await Assert.That(plan.TocEntries.Count).IsEqualTo(2);
        await Assert.That(plan.TocEntries.All(e => e.PageNumber > 0)).IsTrue();
    }
}
