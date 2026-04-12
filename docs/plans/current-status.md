# Current Status

## Last Updated

2026-04-13

## Current Phase

Phase 10: Field Update Engine — **IN PROGRESS**

## Current Step

Steps 10.1–10.11 — **COMPLETE** — Phase 10 is fully implemented.

- All field-update core functionality is implemented: PAGE, NUMPAGES, document properties, TOC, TOF, SEQ, PAGEREF, REF.
- SEQ field update walks all paragraphs in document order, assigns sequential counter values per identifier, handles `\r N` (reset) and `\h` (hidden) switches. Runs before TOC/TOF so caption numbers are fresh for TOF entry text.
- PAGEREF resolves to the page number of the target bookmark via `BookmarkPageMap`; REF resolves to the text content of the target bookmark via `BookmarkTextMap`.
- `FieldUpdateOptions` now has `UpdateSequenceFields` and `UpdateCrossReferences` properties (both default `true`).
- Convergence loop, all field types, and rendering pipeline are fully regression-tested (87 focused tests pass).
- OpenXML-generated corpus documents (with-toc, with-tof, with-cross-refs) created and tested.
- Word Interop field-update corpus generated (field-update-toc, field-update-tof, field-update-page-of, field-update-cross-refs) with Word-rendered reference PNGs. All 4 visual regression tests pass.
- SvgRasterizer hardened to handle empty-content SVG pages (viewBox fallback for 0×0 CullRect).
- DESIGN.md updated with field update architecture and convergence model documentation.
- XML documentation added to all new public/internal types.

## Next Step

Phase 10 is complete. Proceed to Phase 11 (WebAssembly SPA Demo).

## Last Commit

Resolve image/reference issue set and close issue regressions (commit 1aa469f)

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
