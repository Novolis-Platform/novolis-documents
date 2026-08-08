<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents

Book document model for Novolis: trim presets, typography, running chrome, and a closed set of content blocks. No Skia, no Markdown — map sources into `BookDocument` then paginate with `Novolis.Documents.Layout`.

## Install

```bash
dotnet add package Novolis.Documents
```

## Quick start

```csharp
using Novolis.Documents;
using Novolis.Math.Measure;

var book = new BookDocument
{
    Meta = new BookMeta { Title = "Duckville", Author = "Example" },
    Setup = new PageSetup
    {
        Trim = TrimPresets.TradePaperback6x9,
        Margin = TrimPresets.DefaultBookMargin,
    },
    Typography = new Typography(),
    IncludeCover = true,
    IncludeToc = true,
    Header = new RunningChrome { Template = "{title}" },
    Footer = new RunningChrome { Template = "{page}" },
    Body =
    [
        new HeadingBlock { Level = 1, Text = "Chapter 1" },
        new ParagraphBlock { Text = "Once upon a time…" },
    ],
};
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents.Layout` | Paginate into a `PagePlan` |
| `Novolis.Documents.Skia` | Write PDF via SkiaSharp |
| `Novolis.Math.Measure` | Length/Size/Thickness/Rect |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
- Issues: [GitHub Issues](https://github.com/Novolis-Platform/novolis-documents/issues)
