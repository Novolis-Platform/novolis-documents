<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents.Skia

SkiaSharp PDF writer for `PagedDocument`. Implements text measurement for layout and paints First, Toc, body, Last, header/footer bands, tables, columns, images, and watermark. No Skia types leak into `Novolis.Documents` / `.Layout` public APIs.

Embeds a Latin-subset **Liberation Serif** (OFL) by default so short PDFs stay small (~tens of KB) instead of shipping full system Georgia. Override with `DocumentPdfOptions.BodyFontPath` / `BoldFontPath`.

## Install

```bash
dotnet add package Novolis.Documents.Skia
```

Requires .NET 10 (`net10.0`). Restore from nuget.org + GitHub Packages.

## Quick start

```csharp
using Novolis.Documents;
using Novolis.Documents.Skia;

var document = Document.Create("Hello")
    .Page(p => p.Trade6x9().Footer("{page}"))
    .Body(b => b.Content(c => c.Paragraph("Hello, document.")))
    .Build();

DocumentPdf.Write(document, @"C:\Users\frank\.novolis\artifacts\hello.pdf");
var bytes = DocumentPdf.ToBytes(document);
var plan = DocumentPdf.Layout(document);
```

## Docs

- [layout-and-pdf](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/layout-and-pdf.md)
- [getting-started](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/getting-started.md)
- [authoring](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/authoring.md)

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents` | Document model + fluent builders |
| `Novolis.Documents.Layout` | Pagination only (custom measurer) |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
- Issues: [GitHub Issues](https://github.com/Novolis-Platform/novolis-documents/issues)
