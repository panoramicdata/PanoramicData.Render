# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.6 (theme font resolution) — **Complete**

- Updated `FontResolver` to:
  - resolve concrete family names from `ThemeInfo.MajorFont` and `ThemeInfo.MinorFont`
  - select script-specific candidates in order: exact supplemental script font, script-class fallback (`EastAsian` or `ComplexScript`), then Latin/general fallbacks
  - reuse existing substitution, configured fallback, and sans-serif fallback logic after theme candidate selection
- Added script classification for common East Asian and complex-script tags used by OOXML themes
- Expanded `FontResolverTests` to cover:
  - Latin theme font resolution through substitutions
  - exact supplemental script matches
  - East Asian and complex-script fallback selection
  - theme resolution falling through to configured fallback or sans-serif fallback
  - no-candidate behavior when theme and global fallbacks are all unavailable
- 258 total tests passing
- 100% line coverage overall maintained

## Next Step

Step 1.4.7 — Unit tests: verify font resolution, substitution, fallback, and caching

## Last Commit

b83be99 — Implement step 1.4.5: add SKTypeface caching

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
