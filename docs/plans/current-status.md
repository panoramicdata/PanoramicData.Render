# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.5 (SKTypeface creation and caching) — **Complete**

- Updated `FontResolver` to:
  - create `SKTypeface` instances from resolved font files
  - expose `TryGetTypeface(familyName, bold, italic, out SKTypeface?)`
  - cache created typefaces by resolved family and requested bold/italic style
  - reuse cache entries across substitution/fallback resolution when they converge to the same resolved family/style
- Added test seams for deterministic validation:
  - internal constructor overload for metadata-reader and typeface-factory injection
  - coverage for cache reuse, style-specific cache keys, null factory results, and default Skia create success/failure paths
- Expanded `FontResolverTests` to cover:
  - unresolved families do not invoke typeface creation
  - same family/style requests reuse cached typefaces
  - different styles create distinct cache entries
  - substitution and direct-family lookups reuse the same cache entry when they resolve to the same family/style
  - default `SKTypeface.FromFile` creation behavior on valid and invalid font files
- 251 total tests passing
- 100% line coverage overall maintained

## Next Step

Step 1.4.6 — Resolve theme fonts: map `majorFont`/`minorFont` to concrete family names per script

## Last Commit

77c1dc8 — Implement step 1.4.4: add font fallback chain

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
