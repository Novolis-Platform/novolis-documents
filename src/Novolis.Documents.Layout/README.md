<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents.Layout

One-column pagination for `PagedDocument`: First (cover), optional Toc, body flow with H1 page breaks, Last page. Resolves header/footer include flags into `PageSlice.ShowHeader` / `ShowFooter`. Depends on `ITextMeasurer` so unit tests need no Skia.

## Install

```bash
dotnet add package Novolis.Documents.Layout
```

Requires .NET 10 (`net10.0`). Usually pulled transitively by `Novolis.Documents.Skia`.

## Quick start

```csharp
using Novolis.Documents.Layout;

var plan = DocumentPaginator.Paginate(document, measurer);
foreach (var page in plan.Pages)
    Console.WriteLine($"{page.Number}: {page.Kind} ({page.Blocks.Count} blocks)");
```

For a plan that matches PDF paint metrics, prefer `DocumentPdf.Layout` from `Novolis.Documents.Skia`.

## Docs

- [layout-and-pdf](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/layout-and-pdf.md)
- [getting-started](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/getting-started.md)
- [design](https://github.com/Novolis-Platform/novolis-documents/blob/main/docs/design.md)

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents` | Document model + fluent builders |
| `Novolis.Documents.Skia` | PDF paint + real measurer |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
- Issues: [GitHub Issues](https://github.com/Novolis-Platform/novolis-documents/issues)
