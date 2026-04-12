# Current Status

## Last Updated

2026-04-13

## Current Phase

Phase 11: WebAssembly Demo — **IN PROGRESS**

## Current Step

Step 11.1.1 — Browser Feasibility Check — **COMPLETE**

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

## Next Step

Step 11.1.2 → 11.2.1 — Create Demo project scaffold with Wasm shims.

## Last Commit

Complete Phase 10: Field Update Engine (commit a3f3fa6)

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
