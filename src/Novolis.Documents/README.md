<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents

Paged document model for Novolis: trim presets, typography, running chrome, and a closed set of content blocks. No Skia, no Markdown — map sources into `PagedDocument` then paginate with `Novolis.Documents.Layout`.

## Install

```bash
dotnet add package Novolis.Documents
```

## Quick start

```csharp
using Novolis.Documents;
using Novolis.Math.Measure;

var document = new PagedDocument
{
    Meta = new DocumentMeta { Title = "Sample", Author = "Example" },
    Setup = new PageSetup
    {
        Trim = TrimPresets.Inch6x9,
        Margin = TrimPresets.DefaultMargin,
    },
    Typography = new Typography(),
    IncludeCover = true,
    IncludeToc = true,
    First = new FirstPage { Lines = ["Trade sample"] },
    Last = new LastPage { Title = "Colophon", Lines = ["End."] },
    Header = new RunningChrome { Template = "{title}" },
    Footer = new RunningChrome { Template = "{page}" },
    Body =
    [
        new HeadingBlock { Level = 1, Text = "Section One" },
        new ParagraphBlock { Text = "Once upon a time…" },
        new TableBlock
        {
            Headers = ["A", "B"],
            Rows = [["1", "2"]],
        },
    ],
};
```

`DefaultMargin` is print-oriented (binding 0.75″ / outer 0.5″ / head 0.5″ / foot 0.65″). Use `TrimPresets.ReportMargin` for uniform 1″.
## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents.Layout` | Paginate into a `PagePlan` |
| `Novolis.Documents.Skia` | Write PDF via SkiaSharp |
| `Novolis.Math.Measure` | Length/Size/Thickness/Rect |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
- Issues: [GitHub Issues](https://github.com/Novolis-Platform/novolis-documents/issues)
