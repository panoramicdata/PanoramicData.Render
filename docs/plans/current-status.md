# Current Status

## Last Updated

2026-04-13

## Current Phase

Phase 11: WebAssembly Demo — **IN PROGRESS**

## Current Step

Post-demo renderer fidelity remediation — **IN PROGRESS**

### 11.1.1 Feasibility Findings

- **SkiaSharp Wasm**: `SkiaSharp.NativeAssets.WebAssembly` v3.116.1 exists (exact version match). Provides Wasm-compiled native Skia library (~72 MB package).
- **HarfBuzz**: Included in SkiaSharp native Wasm build. `SKShaper` works in browser Wasm.
- **OpenXML**: Pure managed code, stream-based. Fully Wasm-compatible.
- **SVG output**: Pure `StringBuilder` string generation — zero native dependencies. Works out of the box.
- **PDF output**: Uses `SKDocument.CreatePdf(stream)` which depends on native Skia. Works with Wasm native assets.
- **Font loading (current path)**: Render pipeline uses `SKTypeface.FromFamilyName()` (SkiaSharp system font lookup), NOT the dormant `FontResolver`. In Wasm, system fonts are unavailable — SkiaSharp falls back to a default font. Acceptable for MVP.
- **FontEmbedder**: Uses `File.ReadAllBytes()` and `Directory.Exists()` — NOT Wasm-compatible. Must be disabled or shimmed for browser context.
- **FontResolver**: Built but dormant (not wired into production render path). Not a blocker.
- **Blazor Wasm**: `blazorwasm` template available in .NET 10 SDK. Standalone (no server component) is the target.
- **Decision**: Proceed with Blazor WebAssembly standalone. SVG display is the primary UX (browser renders fonts natively). PDF download uses SkiaSharp Wasm with fallback fonts. Font embedding disabled for Wasm builds.

### 11.2 Scaffold Progress

- `PanoramicData.Render.Demo` created as a standalone Blazor WebAssembly app and added to solution.
- Central package management updated for WebAssembly and Skia Wasm native assets.
- Static asset publishing validated via `dotnet publish PanoramicData.Render.Demo/PanoramicData.Render.Demo.csproj -c Release`.
- Base-path handling implemented in `wwwroot/index.html` to support both localhost (`/`) and GitHub Pages repository-path hosting (`/<repo>/`).

### 11.3-11.5 Initial Demo Flow

- Replaced the template home page with a demo-focused intake flow using `InputFile`.
- Added local file validation for `.docx` and `.dotx` and stream-size limits.
- Implemented client-side rendering with `DocxRenderer` and `FieldUpdateOptions` enabled.
- Added a horizontal page-strip "sushi bar" that displays rendered SVG pages with per-page dimensions preserved.
- Added client-side PDF generation and download using a browser-side JS helper and original file stem naming.

### Renderer Fidelity Remediation

- Added a regression test proving paragraph-style-defined run formatting must materialize into SVG output.
- Implemented `StyleCascadeMaterializer` and wired it into `DocxRenderer` before parsing and after field-update passes.
- Updated `RenderCommandEmitter` to preserve run-level text brush/font styling rather than collapsing styled runs to default formatting.
- Replaced the body-table placeholder rectangle branch in `RenderCommandEmitter` with real table parse/layout/paint using the existing table parser and layout helpers.
- Added a focused emitter regression covering table cell background fill, border output, and cell text emission.
- Added a width-aware body-paragraph wrapping slice for simple LTR paragraphs, including pagination metadata so wrapped paragraphs can continue across page boundaries.
- Updated `DocumentLayoutEngine` to measure blocks section-by-section so paragraph height estimation can use real section content widths instead of a document-wide single-line fallback.
- Added regressions for section-aware paragraph measurement, split continuation line indexes, and wrapped paragraph emission.
- Added a temporary performance guard that limits the new body-wrap path to short paragraphs and uses a cheap line-count estimate during measurement so the corpus and aggregate visual suites remain runnable.
- Revalidated the render-emitter test slice, paragraph measurement/splitting slice, the `panoramic-data-document-2026` corpus regression row, and the Word-reference visual comparison slice after the table and paragraph changes.
- Wired image rendering through the full pipeline: `DocxRenderer` now pre-loads all embedded images via `MediaStore` before the OpenXML package is disposed, passes them through `RenderResult` → `RenderedPage` → page renderers → `RenderCommandEmitter`.
- Added `EmitParagraphImages` and `EmitDrawingImage` helpers in `RenderCommandEmitter` that scan each paragraph's `Drawing` elements and emit `DrawImage` calls for both inline images (positioned within the text flow) and anchor/floating images (positioned via `AnchorPositionResolver`).
- Made `RunElementParser.ParseAnchorPlacement` internal so the emitter can reuse anchor placement parsing without duplicating OOXML attribute extraction.
- Added integration tests (`CorpusDocument_WithImages_SvgContainsImageElements`) verifying that the `inline-images` and `floating-images` corpus documents produce SVG output containing `<image>` elements with embedded data URIs.
- Extended `StyleCascadeMaterializer` to also materialize effective ParagraphProperties (alignment, numbering, indentation, spacing) from the style cascade onto each paragraph, not just RunProperties. This fixes missing center alignment and heading numbering that was only defined in paragraph styles.
- Threaded cloned `Styles` element through the render pipeline (`DocxRenderer` → `RenderResult` → `RenderedPage` → `SvgPageRenderer`/`PdfPageRenderer` → `RenderCommandEmitter` → `EmitTableBlock` → `TableLayoutEngine.ComputeCellBackgrounds`) so table-style conditional formatting (banded rows, first-row shading, etc.) can be resolved at render time.
- Changed `TableStyleResolver` and `TableLayoutEngine.ComputeCellBackgrounds` to accept `Styles?` instead of `StyleDefinitionsPart?` so cloned styles survive document disposal.

## Next Step

Begin remediation priority 1 — Heading numbering (A1–A3): parse `lvlText` format strings from OOXML numbering definitions and implement multi-level label concatenation in `ListState` / `ResolveListStyle`. See prioritised plan in `docs/plans/phase-11-webassembly-demo.md` §Renderer Fidelity Remediation Plan.

## Last Commit

Add demo base-path handling for GitHub Pages (commit a2e9221)

## Blockers

None.

## Decisions Made

- .NET 10.0 target (not .NET 8.0 or .NET Standard 2.1 from original brief)
- Knuth-Plass line breaking from day 1 (not greedy)
- Full OOXML style cascade including toggle properties
- High-fidelity goal (not pixel-perfect)
- SkiaSharp for measurement and PDF — accepting known limitations (no tagged PDF, no font subsetting, no PDF/A)
- Library only, no CLI tool
- DOCX only, never .doc
- No macro support
- Visual regression testing: test project may use Word Interop (Microsoft.Office.Interop.Word) to generate ground-truth PNGs for comparison; the main library must NEVER reference Word Interop
- Font embedding via TTF data URIs (pragmatic MVP; WOFF2 upgrade deferred pending library availability)
- Remaining active roadmap is intentionally narrowed to Phase 10 (field updates) and Phase 11 (WebAssembly demo)
- Remaining unfinished non-SPA work is tracked as GitHub backlog issues instead of open phase checklist items
- GitHub Pages deployment should assume a `gh-pages` branch unless repository settings require an Actions-based Pages publish flow instead
- Phase 10 is being implemented as an API-first, test-first series of narrow slices so the default render path stays stable while field recalculation is introduced incrementally
- Existing render-time PAGE and NUMPAGES substitution remains in place; the new field-update loop now also rewrites cached body-field results to support the broader multi-pass field-update architecture
- Document property fields are updated from package core properties via `DocxDocument` accessors, with `RenderOptions.SourceFilename` supplying the browser-upload filename for `FILENAME` fields
- Structural TOC updates require reparsing the body blocks between field-update iterations so inserted/replaced TOC paragraphs participate in measurement and pagination
- TOC `\t` custom-style mappings resolve against the paragraph's direct style ID and the style's human-readable name from the styles part
- TOC `\h` hyperlinks currently target the first preferred bookmark on a heading paragraph, preferring `_Toc*` bookmark names when present and otherwise falling back to the first named bookmark
- When no suitable heading bookmark exists for `\h`, the updater injects a synthetic `_TocGenerated{n}` bookmark and treats that injection as a structural TOC change so the next layout pass emits the named destination
- TOC fidelity now preserves direct paragraph-level tab stop templates from stale TOC result paragraphs, which is enough for the existing renderer to emit leader dots and right-aligned page numbers once the updater uses real `TabChar` elements
- Phase 11 demo uses Blazor WebAssembly standalone with `SkiaSharp.NativeAssets.WebAssembly` for native Skia in browser; SVG rendering is primary display path; font embedding disabled for Wasm context
- While the body parser and paginator still carry tables as `TablePlaceholderBlock`, the emitter now treats those placeholders as a bridge into the existing table parser/layout engine instead of painting a single placeholder rectangle
- Body paragraph wrapping is now enabled only for simple short paragraphs; longer paragraphs still fall back to the legacy single-line path until a reusable line-layout pipeline exists that does not make the aggregate corpus or visual suites unacceptably slow
