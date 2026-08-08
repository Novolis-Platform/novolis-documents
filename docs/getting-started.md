# Getting started

## Prerequisites

- .NET 10 (`net10.0`)
- NuGet sources: **nuget.org** + **GitHub Packages** (`https://nuget.pkg.github.com/Novolis-Platform/index.json`)
- Local multi-repo work: ProjectReference mode via `-p:NovolisUseProjectReferences=true` (see [platform-project-ref-mode](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/platform-project-ref-mode.md))

## Install

```bash
dotnet add package Novolis.Documents
dotnet add package Novolis.Documents.Skia
```

`Novolis.Documents.Layout` is pulled transitively by Skia; reference it directly only if you paginate without writing PDF.

## First PDF (fluent)

```csharp
using Novolis.Documents;
using Novolis.Documents.Skia;

var document = Document.Create("Harbor Notes")
    .Meta(m => m.Author("Novolis").Publisher("Novolis-Platform"))
    .Page(p => p
        .Trade6x9()
        .Header(h => h.Template("{title}").IncludeBody().UseChapterTitle())
        .Footer(f => f.Template("{page} / {pages}").IncludeBody()))
    .Body(b => b
        .First(f => f.Lines("Trade sample"))
        .Content(c => c
            .Toc()
            .Chapter("Arrival", ch => ch
                .Paragraph("The river ran cold through Duckville harbor.")
                .Table(t => t
                    .Headers("Cargo", "Tons")
                    .Row("Grain", "120")
                    .Rules(TableRuleStyle.Horizontal)
                    .HeaderBackground())))
        .Last(l => l.Title("Colophon").Lines("End of sample.")))
    .Build();

DocumentPdf.Write(document, @"C:\Users\frank\.novolis\artifacts\harbor-notes.pdf");
```

## First PDF (object initializer)

Mappers and generators can build `PagedDocument` without the fluent DSL:

```csharp
using Novolis.Documents;
using Novolis.Documents.Skia;

var document = new PagedDocument
{
    Meta = new DocumentMeta { Title = "Hello", Author = "Novolis" },
    Setup = new PageSetup
    {
        Trim = TrimPresets.Inch6x9,
        Margin = TrimPresets.DefaultMargin,
    },
    Typography = new Typography(),
    IncludeCover = true,
    Footer = new Footer
    {
        Template = "{page}",
        IncludeBody = true, // First / Toc / Last default off (same as Header)
    },
    First = new FirstPage { Lines = ["Sample"] },
    Body =
    [
        new HeadingBlock { Level = 1, Text = "One" },
        new ParagraphBlock { Text = "Hello, document." },
    ],
};

DocumentPdf.Write(document, "hello.pdf");
```

## Dogfood samples

| App | Path | Purpose |
| --- | --- | --- |
| HelloDocument | `d:\novolis\novolis-dogfooding\apps\documents\HelloDocument` | Full fluent book-style sample |
| HelloMarkdownPdf | `d:\novolis\novolis-dogfooding\apps\documents\HelloMarkdownPdf` | Markup → Documents → PDF |

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\documents\HelloDocument\HelloDocument.csproj -p:NovolisUseProjectReferences=true
```

Artifact: `C:\Users\frank\.novolis\artifacts\hello-document\hello-document.pdf`

## Build & test this repo

```powershell
dotnet restore d:\novolis\novolis-documents\Novolis.Documents.slnx
dotnet build d:\novolis\novolis-documents\Novolis.Documents.slnx -p:NovolisUseProjectReferences=true
dotnet test d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Novolis.Documents.Unit.csproj -p:NovolisUseProjectReferences=true
```

## Next reading

1. [authoring.md](authoring.md) — fluent DSL reference  
2. [header-footer.md](header-footer.md) — page numbers, chapter header, watermark  
3. [blocks.md](blocks.md) — tables, columns, images  
4. [layout-and-pdf.md](layout-and-pdf.md) — pagination and PDF options  
