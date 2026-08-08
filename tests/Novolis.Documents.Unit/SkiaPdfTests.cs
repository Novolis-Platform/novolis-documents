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
            Footer = new RunningChrome { Template = "{page}" },
            Header = new RunningChrome { Template = "{title}" },
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
}
