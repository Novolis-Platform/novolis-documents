<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents.Skia

SkiaSharp PDF writer for `PagedDocument`. Implements `ITextMeasurer` and paints cover, TOC, body, and chrome. No Skia types leak into `Novolis.Documents` / `.Layout` public APIs.

## Install

```bash
dotnet add package Novolis.Documents.Skia
```

## Quick start

```csharp
using Novolis.Documents.Skia;

DocumentPdf.Write(document, @"C:\temp\out.pdf");
var bytes = DocumentPdf.ToBytes(document);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents` | Document model |
| `Novolis.Documents.Layout` | Pagination only |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
