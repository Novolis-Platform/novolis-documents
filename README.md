<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <strong>Narrow book PDF island</strong><br/>
  Document model, one-column layout, SkiaSharp PDF — not a QuestPDF clone.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-documents/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-documents/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-documents"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Documents` | `dotnet add package Novolis.Documents` | [README](https://github.com/Novolis-Platform/novolis-documents/blob/main/src/Novolis.Documents/README.md) |
| `Novolis.Documents.Layout` | `dotnet add package Novolis.Documents.Layout` | [README](https://github.com/Novolis-Platform/novolis-documents/blob/main/src/Novolis.Documents.Layout/README.md) |
| `Novolis.Documents.Skia` | `dotnet add package Novolis.Documents.Skia` | [README](https://github.com/Novolis-Platform/novolis-documents/blob/main/src/Novolis.Documents.Skia/README.md) |

<!-- novolis-package-index:end -->

# novolis-documents

MIT book PDF island for Novolis: customary trims (6×9 primary), cover / TOC / body / chrome, Skia paint.

See [docs/design.md](docs/design.md) and [docs/getting-started.md](docs/getting-started.md).

## Build

```powershell
dotnet build d:\novolis\novolis-documents\Novolis.Documents.slnx -p:NovolisUseProjectReferences=true
dotnet test d:\novolis\novolis-documents\tests\Novolis.Documents.Unit\Novolis.Documents.Unit.csproj -p:NovolisUseProjectReferences=true
```
