# FAQ

## Is this a QuestPDF / HTML-to-PDF clone?

No. It is a narrow **one-column** flow: immutable blocks → paginate → Skia PDF. No constraint layout, no CSS, no nested cell widgets.

## Should invoices use different types than books?

No. Same `PagedDocument` and blocks. Compose differently (often no First/Toc; A4; columns + tables). Keep invoice vocabulary in your mapper.

## First vs Cover vs IncludeCover?

- Public model: **First** page (`FirstPage`, `IncludeCover`)
- Layout enum: `PageKind.Cover` for that slot (historical name; same page)
- Prefer saying **First** in authoring docs and APIs

## Toc vs Content vs Body?

- **Body** (builder): whole spine First → Content → Last  
- **Content**: main block stream inside Body  
- **Toc**: contents page generated from level-1 / Chapter titles (`IncludeToc`)

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
