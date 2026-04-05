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
```

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **SkiaSharp** for measurement & PDF | MIT-licensed, cross-platform, includes HarfBuzz for complex script shaping |
| **Knuth-Plass** line breaking | Produces paragraph-optimal line breaks matching Word's behaviour more closely than greedy algorithms |
| **Twips** as internal unit | Matches Word's internal precision (1/1440 inch), avoiding accumulated rounding errors |
| **Full OOXML style cascade** | Document Defaults → Theme → Numbering → Table → Paragraph hierarchy → Character hierarchy → Toggle properties → Direct Formatting |

## Supported Formats

| Input | Output |
|---|---|
| `.docx` (OOXML) | SVG (one per page, fonts optionally embedded as WOFF2) |
| | PDF (single file via SkiaSharp PDF backend) |

`.doc` (binary format) is **not supported** and never will be. Future versions may add `.pptx` and `.xlsx` support.

## Requirements

- .NET 10.0+
- Access to `.ttf` and/or `.otf` font files used in the source document (system fonts, a directory, or embedded)

## Documentation

- [DESIGN.md](DESIGN.md) — Architecture and technical design
- [PLAN.md](PLAN.md) — Phased implementation roadmap
- [docs/plans/](docs/plans/) — Detailed per-phase deliverables

## License

[MIT](LICENSE) — Copyright © 2026 Panoramic Data Limited

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and coding standards.
