# Getting started

## Build

```powershell
dotnet restore d:\novolis\novolis-documents\Novolis.Documents.slnx
dotnet build d:\novolis\novolis-documents\Novolis.Documents.slnx -p:NovolisUseProjectReferences=true
dotnet test d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Novolis.Documents.Unit.csproj -p:NovolisUseProjectReferences=true
```

Local multi-repo work uses Platform ProjectReference mode (`NovolisUseProjectReferences=true`) so `Novolis.Math.Measure` resolves from the sibling `novolis-math` checkout. Committed references remain PackageReference-only.

## Minimal PDF

```csharp
using Novolis.Documents;
using Novolis.Documents.Skia;

var book = new BookDocument
{
    Meta = new BookMeta { Title = "Hello" },
    Setup = new PageSetup
    {
        Trim = TrimPresets.TradePaperback6x9,
        Margin = TrimPresets.DefaultBookMargin,
    },
    Typography = new Typography(),
    Footer = new RunningChrome { Template = "{page}" },
    Body =
    [
        new HeadingBlock { Level = 1, Text = "One" },
        new ParagraphBlock { Text = "Hello, book." },
    ],
};

BookPdf.Write(book, "hello.pdf");
```
