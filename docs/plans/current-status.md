# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.3.2 (theme fonts/colors parsing) — **Complete**

- `ThemeInfo` model: major/minor themed font sets + raw theme color map
- `ThemeFontInfo` model: latin/eastAsian/complexScript fonts + supplemental script font map
- `ThemeInfoParser`: parses theme part font scheme and color scheme
  - `Parse(ThemePart?)`: returns empty theme info when theme data is absent
  - Parses `majorFont`/`minorFont` (latin, eastAsian, complexScript, supplemental script mappings)
  - Parses standard color slots: `dk1`, `lt1`, `dk2`, `lt2`, `accent1`-`accent6`, `hlink`, `folHlink`
  - Handles RGB and system colors (using `lastClr` fallback when available)
- Added 8 parser tests and 2 `TestDocxBuilder` helpers for theme scenarios
- 121 total tests passing
- 100% line coverage overall; new parser/model code covered

## Next Step

Step 1.3.3 — Resolve theme colors with tint/shade modifiers to concrete RGB values

## Last Commit

Pending — step 1.3.2 implementation

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
