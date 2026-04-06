# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.7 (font resolver verification tests) — **Complete**

- Expanded `FontResolverTests` to verify the full Phase 1 font infrastructure surface:
  - font directory indexing and TTC family enumeration
  - substitution-based resolution and direct-match precedence
  - configured fallback family and sans-serif fallback resolution
  - `SKTypeface` creation, cache reuse, and style-specific cache separation
  - theme major/minor font resolution by script, including substitution and fallback handoff
- Added explicit `TryGetTypeface` coverage for fallback-family and sans-serif fallback paths so resolution and caching are both verified end-to-end
- Phase 1 font infrastructure now has 260 passing tests across resolver behavior and related font metadata paths
- 100% line coverage overall maintained
- Phase 1 exit criteria are satisfied for font resolution and loading

## Next Step

Step 2.1.1 — Create `MeasurementEngine` class wrapping SkiaSharp: given a `SKTypeface`, font size, and text string, return glyph advance widths

## Last Commit

80d3f4a — Update status after step 1.4.6 commit

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
