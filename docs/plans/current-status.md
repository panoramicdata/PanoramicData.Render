# Current Status

## Last Updated

2026-04-09

## Current Phase

Phase 5: Graphics & Objects — **IN PROGRESS**

## Current Step

Step 5.2.1 — Parse anchor drawing elements (`w:drawing` → `wp:anchor`) — **COMPLETE**

Completed anchor drawing parsing for Phase 5.2.1:
- added `AnchorImageRunElement` model for floating image runs
- extended `RunElementParser` to parse `wp:anchor` drawings (relationship ID, extent, and source crop)
- added parser tests for anchored drawings including standard image, crop parsing, and missing-blip fallback behavior

1510 tests passing via the test runner in this environment.

## Next Step

Step 5.2.2 — Resolve anchor positioning: relative to page, column, paragraph, character, margin

## Last Commit

Step 5.2.1: Parse anchor drawing elements

## Implementation Notes

- `DocxDocument` is internal; test project accesses it via `InternalsVisibleTo`
- `TestDocxBuilder` helper creates minimal and full DOCX files in-memory for tests
- `DocxDocument.Load` disposes the underlying `WordprocessingDocument` on constructor failure (no resource leak)
- Overfull line penalty is 100M demerits to strongly prefer breaking over overflowing
- When no stretch available (ratio > tolerance), accepted with 10K extra demerits
- Application Control policy may block Debug DLLs on this machine; use Release for coverage
- Renamed `HeaderFooterType` to `HeaderFooterKind` to avoid collision with `DocumentFormat.OpenXml.Wordprocessing.HeaderFooterType`
- OpenXML `EnumValue<T>` types cannot be used in C# switch patterns; use `if` chains with `==` instead
- Using TDD + spec-driven development from this point forward

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
