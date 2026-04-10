# Phase 6: Output Drivers

**Depends on:** Phase 2 (Text Layout) — can proceed in parallel with Phases 3–5
**Unlocks:** Phase 8 (Quality & Performance)

## Objective

Implement the `IRenderTarget` abstraction and the SVG and PDF output drivers. A basic SVG driver should be built early (as soon as Phase 2 is complete) to enable visual debugging during subsequent layout phases.

## Steps

### 6.1 Render Target Abstraction

- [x] 6.1.1 — Define the `IRenderTarget` interface: `DrawText`, `DrawLine`, `DrawRect`, `DrawImage`, `DrawPath`, `PushClip`/`PopClip`, `SetHyperlink`
- [x] 6.1.2 — Define supporting types: `RenderFont`, `RenderColor`, `RenderStroke`, `RenderBrush` (solid, gradient)
- [x] 6.1.3 — Implement render command emission from the layout engine: walk positioned elements and emit drawing commands to the target
- [x] 6.1.4 — Unit tests: verify that a mock `IRenderTarget` receives the expected drawing commands for a simple laid-out document

### 6.2 SVG Renderer

- [x] 6.2.1 — Implement `SvgRenderTarget`: builds an SVG XML document from drawing commands
- [x] 6.2.2 — Text rendering: `<text>` elements with explicit `x`/`y` per glyph run (not per glyph — group consecutive glyphs with the same formatting)
- [x] 6.2.3 — Character decorations: underline, strikethrough rendered as `<line>` or `<rect>` elements
- [x] 6.2.4 — Images: embedded as Base64 data URIs in `<image>` elements
- [x] 6.2.5 — Shapes: rendered as `<rect>`, `<ellipse>`, `<path>` elements with fill and stroke
- [x] 6.2.6 — Lines and borders: `<line>` or `<path>` elements with stroke properties
- [x] 6.2.7 — Clipping: `<clipPath>` and `clip-path` attribute
- [x] 6.2.8 — Hyperlinks: `<a xlink:href="...">` wrappers
- [x] 6.2.9 — Page structure: each page is a standalone SVG with `viewBox` set to page dimensions
- [x] 6.2.10 — Unit tests: verify SVG output structure for key rendering scenarios

### 6.3 SVG Font Embedding

- [x] 6.3.1 — Implement TTF font embedding: read `.ttf`/`.otf` font files and encode as Base64 data URIs
- [x] 6.3.2 — Embed fonts as `@font-face` blocks within SVG `<style>` elements
- [x] 6.3.3 — Track fonts per page; only embed fonts actually used on each page
- [x] 6.3.4 — Control via `RenderOptions.EmbedFonts` (default: false)
- [x] 6.3.5 — When fonts are not embedded, reference font families by name in `font-family` attributes
- [x] 6.3.6 — Unit tests: verify font embedding/non-embedding modes produce valid SVG

### 6.4 PDF Renderer

- [x] 6.4.1 — Implement `PdfRenderTarget` using SkiaSharp's `SKDocument.CreatePdf()`
- [x] 6.4.2 — Map `IRenderTarget` drawing commands to SkiaSharp `SKCanvas` API calls
- [x] 6.4.3 — Handle page breaks: `EndPage()` + `BeginPage()` on the `SKDocument`
- [x] 6.4.4 — Text rendering: use `SKCanvas.DrawText()` with correct `SKPaint` configuration
- [x] 6.4.5 — Image rendering: decode image data to `SKBitmap` and draw with `DrawBitmap()`
- [x] 6.4.6 — Handle coordinate system: PDF points (1/72 inch) vs internal twips
- [x] 6.4.7 — Emit PDF metadata: title (from document core properties), author, creation date
- [x] 6.4.8 — Unit tests: verify PDF is well-formed and contains expected page count

### 6.5 Output Options

- [ ] 6.5.1 — Implement `RenderOptions.PageRange`: render only a subset of pages
- [ ] 6.5.2 — Implement `RenderOptions.TargetDpi`: affect SVG `viewBox` and pixel sizes
- [ ] 6.5.3 — Implement `RenderOptions.EmbedImages`: when false, SVG references images by URI instead of data URI
- [ ] 6.5.4 — Unit tests: verify each option produces the expected output variation

## Exit Criteria

- `IRenderTarget` cleanly separates layout from rendering
- SVG output for a multi-page document produces valid, visually correct SVG strings
- SVG fonts can be embedded as WOFF2, producing self-contained SVGs
- PDF output produces a valid PDF file that opens correctly in PDF readers
- Rendering options (`PageRange`, `TargetDpi`, `EmbedFonts`, `EmbedImages`) function correctly
- All tests pass; zero warnings

## Known Risks

- WOFF2 compression requires a native library or a .NET port. Options: Google's `woff2` library via P/Invoke, or a managed implementation. Investigate availability before committing to this approach.
- SkiaSharp's PDF backend does not support font subsetting — PDF files may be large when many fonts are used.
- SVG text positioning per glyph run (not per glyph) may produce slight visual differences with complex scripts; may need to fall back to per-glyph positioning for non-Latin scripts.
- SkiaSharp `SKDocument.CreatePdf()` has limited PDF feature support — no bookmarks, no tagged PDF, no PDF/A. These are documented as known limitations.
