# Header, footer, and watermark

Page bands sit outside the content box. Layout reserves `PageSetup.HeaderBand` / `FooterBand`; Skia paints text into those bands when a page’s `ShowHeader` / `ShowFooter` flags are set.

## Include flags

Both `Header` and `Footer` use the same include model (defaults differ):

| Flag | Page kind | Header default | Footer default |
| --- | --- | --- | --- |
| `IncludeFirstPage` | Opening / title (`PageKind.Cover`) | off | on |
| `IncludeToc` | TOC pages | off | on |
| `IncludeBody` | Main flow | on | on |
| `IncludeLastPage` | Closing page | off | on |

Fluent builders mirror these names:

```csharp
.Header(h => h
    .Template("{title}")
    .IncludeBody()
    .UseChapterTitle())
.Footer(f => f
    .Template("{page} / {pages}")
    .IncludeFirstPage()
    .IncludeToc()
    .IncludeBody()
    .IncludeLastPage())
```

Shortcuts on `DocumentPageBuilder`:

- `.Header("{title}")` → template + body only  
- `.Footer("{page} / {pages}")` → template + First + Toc + Body + Last  

## Templates

Supported placeholders (case-sensitive):

| Token | Source |
| --- | --- |
| `{page}` | 1-based page number |
| `{pages}` | Total page count |
| `{title}` | `Meta.Title` |
| `{subtitle}` | `Meta.Subtitle` |
| `{author}` | `Meta.Author` |
| `{series}` | `Meta.Series` |
| `{publisher}` | `Meta.Publisher` |
| `{subject}` | `Meta.Subject` |
| `{identifier}` | `Meta.Identifier` |
| `{version}` | `Meta.Version` |
| `{language}` | `Meta.Language` |
| `{date}` | `Meta.Date` (formatted) |
| `{rights}` | `Meta.Rights` |
| `{chapter}` | Current chapter title (body pages) |

Header and footer text are centered in their bands. Font size: `FontSizePt` (default 9).

## Chapter title header

```csharp
.Header(h => h.Template("{title}").IncludeBody().UseChapterTitle())
```

When `UseChapterTitle` is true, body pages paint the **current chapter title** (last level-1 heading) instead of expanding the template. If no chapter is active yet, the template is used.

`PageSlice.ChapterTitle` carries that string for layout/PDF.

## Band heights

```csharp
.Page(p => p.Bands(headerPt: 16f, footerPt: 16f))
```

If the band is too small for the font, glyphs may clip — raise the band or lower `FontSizePt`.

## Watermark

Diagonal text painted **under** content (after white clear, before blocks).

```csharp
.Watermark(w => w
    .Text("DRAFT")
    .Color(DocumentColor.Red)   // default
    .Opacity(0.12f)
    .FontSize(54f)
    .Rotation(-32f)
    .On(WatermarkPages.All))
```

| Property | Default |
| --- | --- |
| `Text` | required (`DRAFT` in builder) |
| `Color` | `DocumentColor.Red` |
| `Opacity` | `0.12` |
| `FontSizePt` | `54` |
| `RotationDegrees` | `-32` |
| `Pages` | `WatermarkPages.All` |

`WatermarkPages` is a flags enum: `First`, `Toc`, `Body`, `Last`, `All`.

Named colors: `DocumentColor.Red`, `.Gray`, `.Black`, plus `FromRgb` / `Parse("#RRGGBB")`.

Remove with `.NoWatermark()` or leave `PagedDocument.Watermark` null.

## Naming notes

- Types are **`Header`** and **`Footer`** — not “RunningHeader”, not “Chrome”.
- **First** page (not “cover” in the public model; layout still uses `PageKind.Cover` internally for that slot).
- **Toc** is the contents page; the main flow is **Content** / **Body**.
