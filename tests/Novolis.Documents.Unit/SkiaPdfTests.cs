using Novolis.Documents;
using Novolis.Documents.Skia;
using TUnit.Core;

namespace Novolis.Documents.Unit;

public sealed class SkiaPdfTests
{
    [Test]
    public async Task ToBytes_trade_paperback_has_cover_chapters_and_bytes()
    {
        var book = new BookDocument
        {
            Meta = new BookMeta { Title = "Duckville", Author = "Tester" },
            Setup = new PageSetup
            {
                Trim = TrimPresets.TradePaperback6x9,
                Margin = TrimPresets.DefaultBookMargin,
            },
            Typography = new Typography(),
            IncludeCover = true,
            IncludeToc = true,
            Footer = new RunningChrome { Template = "{page}" },
            Header = new RunningChrome { Template = "{title}" },
            Body =
            [
                new HeadingBlock { Level = 1, Text = "Chapter One" },
                new ParagraphBlock { Text = "The river ran cold through Duckville." },
                new HeadingBlock { Level = 1, Text = "Chapter Two" },
                new ParagraphBlock { Text = "Morning light found the bridge empty." },
            ],
        };

        var bytes = BookPdf.ToBytes(book);
        await Assert.That(bytes.Length).IsGreaterThan(500);
        // PDF magic
        await Assert.That(bytes[0]).IsEqualTo((byte)'%');
        await Assert.That(bytes[1]).IsEqualTo((byte)'P');
    }
}
