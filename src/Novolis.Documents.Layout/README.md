<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-documents/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-documents/) · [Source](https://github.com/Novolis-Platform/novolis-documents)
<!-- novolis-pkg-brand:end -->

# Novolis.Documents.Layout

Renderer-independent pagination for `Document.Sections`: First, body, and Last page roles,
semantic fragments, row-based table splitting, and resolved header/footer flags. Depends on
`ITextMeasurer` so unit tests need no Skia.

## Install

```bash
dotnet add package Novolis.Documents.Layout
```

Requires .NET 10 (`net10.0`). Usually pulled transitively by `Novolis.Documents.Skia`.

## Quick start

```csharp
using Novolis.Documents.Layout;

var plan = SectionPaginator.Paginate(document, measurer);
foreach (var page in plan.Pages)
    Console.WriteLine($"{page.Number}: {page.Kind} ({page.Fragments.Count} fragments)");
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
