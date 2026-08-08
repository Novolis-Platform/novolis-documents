# Document model

`PagedDocument` is the immutable root. Builders and mappers both produce this shape; layout and PDF only consume it.

## PagedDocument

| Property | Role |
| --- | --- |
| `Meta` | Title and bibliographic fields |
| `Setup` | Trim size, margins, header/footer band heights |
| `Typography` | Font sizes and spacing |
| `Body` | Main flow blocks (not First/Last content) |
| `Header` / `Footer` | Optional page header/footer |
| `Watermark` | Optional diagonal text |
| `First` | Optional title-page overrides + lines |
| `IncludeCover` | Emit First page from Meta when true |
| `IncludeToc` | Emit TOC page(s) from level-1 titles |
| `Last` | Optional closing page |
| `HasFirstPage` | `IncludeCover \|\| First is not null` |

## DocumentMeta

| Field | Notes |
| --- | --- |
| `Title` | Required |
| `Subtitle`, `Series` | Displayed on First when set |
| `Author`, `Contributors` | People lines |
| `Publisher` | Imprint |
| `Subject`, `Description` | Topic / abstract |
| `Keywords` | `IReadOnlyList<string>` |
| `Identifier` | ISBN, DOI, internal id, … |
| `Language` | BCP-47 style tag (`en`, `nb-NO`) |
| `Version` | Edition label |
| `Date` | `DateOnly?` |
| `Rights` | Copyright line |

## PageSetup

| Property | Meaning |
| --- | --- |
| `Trim` | Finished page size (`Size` from Math.Measure) |
| `Margin` | Content margins (`Thickness`) |
| `HeaderBand` / `FooterBand` | Reserved vertical bands (`Length`) |

### TrimPresets

| Preset | Size |
| --- | --- |
| `Inch6x9` | 6×9″ (trade default) |
| `Inch5_5x8_5` | 5.5×8.5″ |
| `A5` | ISO A5 |
| `A4` | ISO A4 |
| `USLetter` | US Letter |
| `DefaultMargin` | Print-oriented: binding 0.75″ / outer 0.5″ / head 0.5″ / foot 0.65″ |
| `ReportMargin` | Uniform 1″ |

## Typography

| Property | Default role |
| --- | --- |
| `BodyFontFamily` | Informational name; Skia embeds Liberation Serif unless overridden |
| `BodyFontSizePt`, `H1SizePt`–`H3SizePt` | Type scale |
| `TableFontSizePt` | ≤0 → body size (`EffectiveTableFontSizePt`) |
| `LineHeight` | Multiplier |
| `ParagraphSpacingPt` | After most blocks |
| `AfterLevel1SpacingPt` / `AfterHeadingSpacingPt` | After headings |
| `TableCellPaddingPt` / `TableRuleStrokePt` | Table paint |

## FirstPage / LastPage

**FirstPage** — optional overrides for title/subtitle/series/author/rights, plus centered `Lines`. Missing fields fall back to `Meta`.

**LastPage** — optional `Title`, plain `Lines`, and richer `Blocks` after the lines.

## Immutability

All model types use `init` / `required`. Treat instances as frozen after construction. Builders allocate new graphs on `Build()`.
