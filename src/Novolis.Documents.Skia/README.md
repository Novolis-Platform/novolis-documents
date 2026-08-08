<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents.Skia

SkiaSharp PDF writer for `BookDocument`. Implements `ITextMeasurer` and paints cover, TOC, body, and chrome. No Skia types leak into `Novolis.Documents` / `.Layout` public APIs.

## Install

```bash
dotnet add package Novolis.Documents.Skia
```

## Quick start

```csharp
using Novolis.Documents.Skia;

BookPdf.Write(book, @"C:\temp\book.pdf");
var bytes = BookPdf.ToBytes(book);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents` | Book model |
| `Novolis.Documents.Layout` | Pagination only |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
