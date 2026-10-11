# FAQ

## Is this a QuestPDF / HTML-to-PDF clone?

No. It is a narrow vertical flow: immutable sections → paginate → Skia PDF. No constraint layout, no CSS, no nested cell widgets.

## Should invoices use different types than books?

No. The same `Document`, sections, and content types support both. Compose invoices with A4,
columns, tables, and mapper-owned billing vocabulary.

## First vs Cover vs IncludeCover?

- Public model: **First** page (`FirstPage`, `IncludeCover`)
- Layout enum: `PageKind.First` for that slot (`PageKind.Cover` is a compatibility alias)
- Prefer saying **First** in authoring docs and APIs

## Toc vs Content vs Body?

- **Sections**: main vertical document flow  
- **Body**: compatibility builder surface  
- **Toc**: outline entries generated from level-one headings (`IncludeToc`)

## Why no Avalonia?

Documents is an orthogonal island. Avalonia hosts may *call* `DocumentPdf`; they must not pull Avalonia into these packages (platform stack rules).

## Local build without waiting for GPR?

Open / build via ProjectReference mode:

```powershell
dotnet build d:\novolis\novolis-documents\Novolis.Documents.slnx -p:NovolisUseProjectReferences=true
```

Or the platform meta solution. Do not add a local NuGet folder feed.

## Where do samples live?

Dogfooding, not this library repo:

- `d:\novolis\novolis-dogfooding\apps\documents\HelloDocument`
- `d:\novolis\novolis-dogfooding\apps\documents\HelloMarkdownPdf`
