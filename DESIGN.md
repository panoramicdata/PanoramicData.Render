# PanoramicData.Render — Design Document

## 1. Vision

A 100% open-source, high-fidelity rendering engine for OpenXML documents. PanoramicData.Render acts as a virtual layout engine, calculating exact glyph positions, line breaks, and object anchors to produce a visually faithful representation of the document in SVG and PDF formats.

**Fidelity goal:** "Visually indistinguishable at normal zoom" — not pixel-identical with Microsoft Word, but close enough that a human viewer cannot tell the difference without overlaying outputs and zooming.

**Primary use case:** Server-side or client-side DOCX-to-SVG conversion for web-based document viewing. The library produces SVG strings and PDF streams; UI and visualization are the consumer's responsibility.

## 2. Architecture Overview

The library follows a **Measure-then-Paint** pipeline with strict separation between layout computation and output rendering.

```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────┐    ┌──────────────┐    ┌───────────────┐
│   OpenXML    │───>│  Style           │───>│  Layout Engine  │───>│  Render      │───>│  Output       │
│   Ingestion  │    │  Resolution      │    │  (The "Brain")  │    │  Abstraction │    │  Drivers      │
└─────────────┘    └──────────────────┘    └─────────────────┘    └──────────────┘    └───────────────┘
     │                    │                       │                      │                    │
  Open-XML-SDK     Full OOXML cascade       SkiaSharp metrics      IRenderTarget        SVG / PDF
  DOM loading      Theme → Direct fmt       Knuth-Plass breaks    Drawing commands     One per page
```

### 2.1 Pipeline Stages

#### Stage 1: DOM Ingestion

Uses the Open-XML-SDK (`DocumentFormat.OpenXml`) to load document parts:

- **Document body** — paragraphs, tables, content controls
- **Styles** — style definitions with `basedOn` chains
- **Theme** — theme colors, theme fonts, tint/shade modifiers
- **Numbering** — multi-level list definitions, abstract numbering, number format overrides
- **Settings** — default tab stop, compatibility settings, document grid
- **Headers/Footers** — per-section, with first-page and odd/even variants
- **Relationships** — images, hyperlinks, OLE objects
- **Embedded media** — images stored in the package

#### Stage 2: Style Resolution

A cascading engine that resolves the **effective formatting** for every text run. The full OOXML cascade order:

1. **Document Defaults** (`w:docDefaults`) — base paragraph and run properties
2. **Theme** — theme fonts (`majorFont`/`minorFont`), theme colors with tint/shade
3. **Numbering Styles** — formatting inherited from list level definitions
4. **Table Styles** — conditional formatting bands (first row, last column, etc.)
5. **Paragraph Style Hierarchy** — `basedOn` chains (potentially 10+ levels deep)
6. **Character Style Hierarchy** — `basedOn` chains for run-level styles
7. **Toggle Properties** — `w:b`, `w:i`, `w:caps`, `w:smallCaps` toggle rather than set when the inherited value is already `true`
8. **Direct Formatting** — explicit properties on the paragraph/run element
9. **Revision Overrides** — tracked change formatting (rendered as final state; revision marks not displayed)

**Key complexity:** Toggle properties. `<w:b/>` on a run inside a bold character style _toggles_ bold **off**, not reinforces it. This applies to: bold, italic, caps, smallCaps, strike, dstrike, vanish, emboss, imprint, outline, shadow.

#### Stage 3: The Layout Engine

The computational core. Iterates through resolved blocks and computes exact positions.

**Units:** All internal calculations use **twips** (1/1440 inch) to match Word's internal precision. Conversion to output units (SVG px, PDF points) happens only at render time.

**Font Metrics:** Uses SkiaSharp with HarfBuzz (`SKShaper`) for:

- Glyph measurement (advance widths, ink bounds)
- Complex script shaping (Arabic, Devanagari, Thai, etc.)
- Kerning pair adjustments
- Ligature substitution

**Line Breaking:** Implements the **Knuth-Plass** algorithm from day one:

- Considers the entire paragraph to minimize overall "badness"
- Supports hyphenation via TeX hyphenation patterns (optional)
- Handles justification by distributing space across glue items
- Produces line breaks that closely match Word's output on justified text

**Pagination:** Determines page breaks based on:

- Page dimensions and margins (per section)
- Widow/orphan control
- Keep-with-next / keep-lines-together
- Section breaks (next page, continuous, odd/even)
- Fixed page break characters
- Footnote/endnote space reservation

#### Stage 4: Render Abstraction

An `IRenderTarget` interface that accepts drawing commands:

- `DrawText(glyphs, positions, font, color)` — positioned glyph run
- `DrawLine(from, to, stroke)` — line segment
- `DrawRect(rect, fill, stroke)` — rectangle (borders, backgrounds)
- `DrawImage(data, rect)` — raster image
- `DrawPath(path, fill, stroke)` — arbitrary vector path
- `PushClip(rect)` / `PopClip()` — clipping regions
- `SetHyperlink(rect, uri)` — clickable region

This abstraction decouples layout from output format — the layout engine emits drawing commands without knowing whether the target is SVG, PDF, or something else.

#### Stage 5: Output Drivers

**SvgRenderer:**

- Generates one SVG string per page
- Fonts optionally embedded as Base64 WOFF2 in `<style>` blocks
- Text positioned via `<text>` elements with explicit `x`/`y` per glyph run
- Hyperlinks emitted as `<a>` wrappers
- Images embedded as Base64 data URIs

**PdfRenderer:**

- Uses SkiaSharp's PDF backend (`SKDocument`)
- Generates a single PDF file with one page per document page
- Font embedding handled by SkiaSharp (note: no font subsetting or tagged PDF — known limitations)

## 3. Font Handling

### 3.1 Font Sources

Fonts are resolved in priority order:

1. **Fonts embedded in the DOCX** (if present in the package)
2. **Configured font directories** (via `RenderOptions.FontDirectories`)
3. **System font directories** (platform-dependent defaults)

### 3.2 Supported Formats

- `.ttf` — TrueType fonts
- `.otf` — OpenType fonts
- `.ttc` — TrueType Collections (multiple faces per file, common for CJK)

### 3.3 Font Fallback

When a requested font is not available:

1. Check `RenderOptions.FontSubstitutions` for explicit mapping (e.g., `"Calibri" → "Liberation Sans"`)
2. Fall back to `RenderOptions.FallbackFontFamily`
3. If still unresolved, log a warning and use the first available sans-serif font

### 3.4 Font Licensing

Embedding fonts (SVG WOFF2, PDF) may violate font licenses. This is the **caller's responsibility**. The library provides `RenderOptions.EmbedFonts` to control this behaviour.

## 4. Public API Surface

```csharp
/// <summary>
/// Configuration for document rendering.
/// </summary>
public class RenderOptions
{
    /// <summary>Directories to search for font files.</summary>
    public List<string> FontDirectories { get; set; }

    /// <summary>Explicit font name substitutions (key=requested, value=replacement).</summary>
    public Dictionary<string, string> FontSubstitutions { get; set; }

    /// <summary>Font family to use when no match is found.</summary>
    public string FallbackFontFamily { get; set; }

    /// <summary>Target DPI for SVG output (default: 96).</summary>
    public double TargetDpi { get; set; }

    /// <summary>Whether to embed fonts in SVG output as WOFF2 (default: false).</summary>
    public bool EmbedFonts { get; set; }

    /// <summary>Whether to embed images as data URIs in SVG (default: true).</summary>
    public bool EmbedImages { get; set; }

    /// <summary>Optional page range to render (null = all pages).</summary>
    public Range? PageRange { get; set; }
}

/// <summary>
/// Main entry point for rendering DOCX documents.
/// </summary>
public class DocxRenderer
{
    public DocxRenderer(RenderOptions options);
    public DocxRenderer(RenderOptions options, ILogger logger);

    public Task<RenderResult> RenderAsync(
        Stream docxStream,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of rendering a document.
/// </summary>
public class RenderResult
{
    public IReadOnlyList<RenderedPage> Pages { get; }

    public Task ToPdfAsync(
        Stream output,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single rendered page.
/// </summary>
public class RenderedPage
{
    public double WidthPoints { get; }
    public double HeightPoints { get; }

    public string ToSvg();
}
```

### 4.1 Design Principles

- **Immutable results.** `RenderResult` and `RenderedPage` are immutable once produced.
- **Async throughout.** I/O-bound operations (reading streams, writing PDF) are async with `CancellationToken`.
- **No global state.** `DocxRenderer` is stateless after construction; safe to reuse across calls.
- **Logging via `ILogger`.** No `Console.Write` or `Trace`; structured logging only.

## 5. Non-Functional Requirements

### 5.1 Performance

| Metric | Target |
|---|---|
| Simple 1-page document | < 500ms |
| 50-page business report | < 10s |
| 500-page document | < 120s |
| Throughput (concurrent) | Linear scaling up to CPU core count |

### 5.2 Memory

- Peak memory should not exceed **3× the DOCX file size** for text-heavy documents
- Image-heavy documents may use more; images are streamed where possible rather than buffered entirely
- No memory leaks on repeated renders (verified via long-running tests)

### 5.3 Thread Safety

- `DocxRenderer` is thread-safe: multiple documents can render concurrently
- `RenderResult` is immutable and safe to read from multiple threads
- Font caches are shared (thread-safe) across renders for efficiency

### 5.4 Error Tolerance

- **Malformed DOCX files:** Best-effort rendering with warnings logged, not exceptions thrown
- **Missing fonts:** Substitution with fallback + logged warning
- **Unsupported features:** Rendered as empty space or placeholder with logged warning; never crashes
- **Corrupt images:** Replaced with a placeholder rectangle

### 5.5 Cancellation

`CancellationToken` is threaded through all pipeline stages. Cancellation is checked:

- Between pages during pagination
- Between paragraphs during layout
- During font loading
- During output generation

## 6. Explicitly Out of Scope

| Feature | Status |
|---|---|
| `.doc` (binary Word format) | Never supported |
| Macro execution (`.docm`) | Macros stripped; content rendered |
| Document editing / round-trip | Read-only rendering only |
| HTML output | Not planned |
| CLI tool | Not planned (library only) |
| SmartArt | Future phase — best-effort fallback image if available |
| OLE embedded objects | Future phase — best-effort fallback image if available |
| Revision marks / comments display | Renders final document state only |
| Accessible / tagged PDF | Known SkiaSharp limitation; not in scope |
| PDF/A compliance | Known SkiaSharp limitation; not in scope |
| Font subsetting for PDF | Known SkiaSharp limitation; not in scope |

## 7. Dependencies

| Package | Purpose | License |
|---|---|---|
| `DocumentFormat.OpenXml` | OOXML parsing | MIT |
| `SkiaSharp` | Font metrics, image processing, PDF backend | MIT |
| `SkiaSharp.HarfBuzz` | Complex script shaping, kerning | MIT |
| `Microsoft.Extensions.Logging.Abstractions` | Structured logging | MIT |

All dependencies are MIT-licensed, consistent with this project's MIT license.

## 8. Testing Strategy

### 8.1 Unit Tests

- **StyleResolverTests** — Verify cascade resolution: toggle properties, basedOn chains, theme inheritance
- **TwipConverterTests** — Ensure rounding errors don't accumulate over long documents
- **KnuthPlassTests** — Verify line break positions against known-good outputs
- **FontResolverTests** — Fallback chains, substitution mappings, missing font handling

### 8.2 Layout Tests

Deterministic tests that verify computed positions:

- "Given this paragraph with these styles and this page width, line breaks occur at word indices [X, Y, Z]"
- "Given this table with these column widths, cell (2,3) is positioned at (x, y) with dimensions (w, h)"

These are fast, run without image comparison, and are the primary diagnostic tool.

### 8.3 Visual Regression Tests

Image comparison for end-to-end fidelity:

- **Baseline:** Reference PNGs generated from a controlled Word version (pinned, documented)
- **Test:** PanoramicData.Render generates SVG → rasterized to PNG at 150 DPI
- **Comparison:** Perceptual diff (not raw pixel comparison) with configurable threshold
- **Threshold:** Tests fail if perceptual difference exceeds a defined threshold per test document
- **Tool:** A perceptual diff library (e.g., pixelmatch-style algorithm) to avoid false positives from anti-aliasing

### 8.4 Test Document Corpus

A curated set of `.docx` files covering:

- Basic text formatting (bold, italic, sizes, colors)
- Paragraph alignment and indentation
- Multi-level numbered and bulleted lists
- Tables (simple, merged cells, nested, auto-fit)
- Headers, footers, page numbers
- Sections with different page sizes/orientations
- Inline and floating images
- Tab stops and leaders
- Footnotes and endnotes
- Columns
- Watermarks
- RTL text
- Complex scripts (Arabic, CJK)

Each test document is small and tests one feature to keep failures diagnostic.
