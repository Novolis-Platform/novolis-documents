<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Documents.Layout

One-column book pagination: cover, optional TOC, H1 page breaks, running chrome flags. Depends on `ITextMeasurer` so unit tests need no Skia.

## Install

```bash
dotnet add package Novolis.Documents.Layout
```

## Quick start

```csharp
using Novolis.Documents.Layout;

var plan = BookPaginator.Paginate(book, measurer);
foreach (var page in plan.Pages)
    Console.WriteLine($"{page.Number}: {page.Kind} ({page.Blocks.Count} blocks)");
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Documents` | Book model |
| `Novolis.Documents.Skia` | PDF paint + real measurer |

## Support

- Docs: [novolis-documents](https://github.com/Novolis-Platform/novolis-documents)
