# PanoramicData.Render

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/placeholder)](https://app.codacy.com/gh/panoramicdata/PanoramicData.Render/dashboard)
[![NuGet](https://img.shields.io/nuget/v/PanoramicData.Render)](https://www.nuget.org/packages/PanoramicData.Render/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-fidelity rendering engine for OpenXML (DOCX) documents, producing paginated SVG and PDF output.

## What It Does

PanoramicData.Render is a **virtual layout engine** that reads `.docx` files and produces visually faithful SVG pages (one per document page) and PDF files. Unlike semantic converters that map document tags to HTML, this library calculates exact glyph positions, line breaks, and object anchors — acting as a layout engine comparable to a word processor's print path.

## What It Is Not

- **Not a DOCX editor.** This is read-only, one-way conversion.
- **Not an HTML converter.** Output is SVG and PDF only; there is no HTML mapping.
- **Not pixel-perfect.** The goal is "visually indistinguishable at normal zoom." Sub-pixel identity with Microsoft Word is not claimed.
- **Not a UI component.** This is a library that produces files/strings. Visualization is the consumer's responsibility.

## Quick Start

```csharp
using PanoramicData.Render;

var options = new RenderOptions
{
    FontDirectories = ["/usr/share/fonts", "C:\\Windows\\Fonts"],
    FallbackFontFamily = "Liberation Sans",
    TargetDpi = 96
};

await using var stream = File.OpenRead("document.docx");
var renderer = new DocxRenderer(options);
var result = await renderer.RenderAsync(stream);

// Get SVG for each page
for (var i = 0; i < result.Pages.Count; i++)
{
    var svg = result.Pages[i].ToSvg();
    await File.WriteAllTextAsync($"page-{i + 1}.svg", svg);
}

// Or export as a single PDF
await using var pdfStream = File.Create("output.pdf");
await result.ToPdfAsync(pdfStream);

// Or get PDF bytes directly
var pdfBytes = result.ToPdf();
```

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **SkiaSharp** for measurement & PDF | MIT-licensed, cross-platform, includes HarfBuzz for complex script shaping |
| **Knuth-Plass** line breaking | Produces paragraph-optimal line breaks matching Word's behaviour more closely than greedy algorithms |
| **Twips** as internal unit | Matches Word's internal precision (1/1440 inch), avoiding accumulated rounding errors |
| **Full OOXML style cascade** | Document Defaults → Theme → Numbering → Table → Paragraph hierarchy → Character hierarchy → Toggle properties → Direct Formatting |

## Configuration

`RenderOptions` supports these settings:

| Property | Default | Description |
|---|---|---|
| `FontDirectories` | `[]` | Directories to search for `.ttf`/`.otf` font files |
| `FallbackFontFamily` | `""` | Font to use when the document's font cannot be resolved |
| `TargetDpi` | `96` | Target DPI for SVG output scaling |
| `EmbedFonts` | `false` | Embed fonts as WOFF2 `@font-face` in SVG output |
| `EmbedImages` | `true` | Embed images as data URIs in SVG output |
| `EnableHyphenation` | `false` | Enable automatic hyphenation using TeX patterns |
| `PageRange` | `null` | Optional `Range` to render a subset of pages |
| `ShowHiddenText` | `false` | Include runs with `w:vanish` in the layout |

## Supported Formats

| Input | Output |
|---|---|
| `.docx` (OOXML) | SVG (one per page, fonts optionally embedded as WOFF2) |
| | PDF (single file via SkiaSharp PDF backend) |

`.doc` (binary format) is **not supported** and never will be. Future versions may add `.pptx` and `.xlsx` support.

## Requirements

- .NET 10.0+
- Access to `.ttf` and/or `.otf` font files used in the source document (system fonts, a directory, or embedded)

## Demo App (Local)

The repository includes a Blazor WebAssembly demo app in `PanoramicData.Render.Demo`.

### 1) Install WebAssembly tooling

The demo requires the .NET WebAssembly workload:

```powershell
dotnet workload install wasm-tools
```

If you have multiple SDK feature bands installed and need to align workloads:

```powershell
dotnet workload restore PanoramicData.Render.Demo/PanoramicData.Render.Demo.csproj
```

### 2) Build and run the demo

From repo root:

```powershell
dotnet run --project PanoramicData.Render.Demo/PanoramicData.Render.Demo.csproj --configuration Debug --urls "http://localhost:5250"
```

Then open:

- `http://localhost:5250`

### 3) Optional release publish (static output)

```powershell
dotnet publish PanoramicData.Render.Demo/PanoramicData.Render.Demo.csproj --configuration Release
```

Published web assets are in:

- `PanoramicData.Render.Demo/bin/Release/net10.0/publish/wwwroot`

## GitHub Pages Deployment (Demo)

CI is configured to publish the demo app to GitHub Pages from GitHub Actions on pushes to `main` and on semantic version tags.

### Triggering a deployment with a tag

```powershell
git tag 1.0.2
git push origin 1.0.2
```

### Verifying deployment status

Use GitHub UI:

- Actions -> latest `CI` run -> `deploy_pages` job

Or with `gh` CLI:

```powershell
gh run list --workflow CI --limit 5
gh run view <run-id> --log
```

## Custom Domain: https://render.panoramicdata.com

The repository deploys a `CNAME` file for `render.panoramicdata.com` as part of the Pages artifact.

### 1) DNS

Create a DNS record:

- Type: `CNAME`
- Host: `render`
- Target: `panoramicdata.github.io`

### 2) GitHub Pages settings

In repo Settings -> Pages:

- Source: `GitHub Actions`
- Custom domain: `render.panoramicdata.com`
- Enable `Enforce HTTPS` after DNS is validated

### 3) Optional `gh` commands

Set custom domain via API:

```powershell
gh api --method PUT repos/panoramicdata/PanoramicData.Render/pages -f cname='render.panoramicdata.com'
```

Read current Pages config:

```powershell
gh api repos/panoramicdata/PanoramicData.Render/pages
```

### SSL/TLS certificate notes

You do not upload a certificate manually for GitHub Pages custom domains. After DNS and CNAME are correct, GitHub automatically provisions and renews TLS certificates.

## Documentation

- [DESIGN.md](DESIGN.md) — Architecture and technical design
- [PLAN.md](PLAN.md) — Phased implementation roadmap
- [docs/supported-features.md](docs/supported-features.md) — Supported features matrix
- [docs/known-limitations.md](docs/known-limitations.md) — Known limitations
- [docs/plans/](docs/plans/) — Detailed per-phase deliverables

## License

[MIT](LICENSE) — Copyright © 2026 Panoramic Data Limited

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and coding standards.
