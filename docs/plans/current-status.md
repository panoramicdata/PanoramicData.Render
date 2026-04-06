# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.4 (font fallback chain) — **Complete**

- Updated `FontResolver` fallback behavior to resolve fonts in this order:
  - requested family
  - explicit substitution from `RenderOptions.FontSubstitutions`
  - configured `RenderOptions.FallbackFontFamily`
  - first available sans-serif family
- Added a deterministic sans-serif preference list with a secondary `"sans"` name heuristic for indexed families outside the preferred set
- Expanded `FontResolverTests` to cover:
  - configured fallback-family resolution
  - fallback after a missing substitution target
  - preferred sans-serif fallback
  - non-preferred sans-serif heuristic fallback
  - no-match behavior when no fallback candidate exists
- 243 total tests passing
- 100% line coverage overall maintained

## Next Step

Step 1.4.5 — Create `SKTypeface` instances from resolved font files; cache by family+style for reuse

## Last Commit

398f6bd — Implement step 1.4.3: add font substitution mapping

## Implementation Notes

- `DocxDocument` is internal; test project accesses it via `InternalsVisibleTo`
- `TestDocxBuilder` helper creates minimal and full DOCX files in-memory for tests
- `DocxDocument.Load` disposes the underlying `WordprocessingDocument` on constructor failure (no resource leak)
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
