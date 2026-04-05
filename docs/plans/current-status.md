# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.3.3 (theme color tint/shade resolution) — **Complete**

- `ThemeColorResolver`: resolves theme slots + modifiers to concrete RGB hex values
  - `Resolve(ThemeInfo, ThemeColorValues?, themeTint, themeShade)`: resolves from theme slot map
  - Handles standard slot mapping (`dk/lt`, `accent1`-`accent6`, hyperlinks)
  - Applies `themeShade` darkening and `themeTint` lightening modifiers (byte-hex semantics)
  - Returns `null` for missing slots, non-RGB base values, or invalid modifiers/base color formats
- Added 14 resolver tests covering mapping, tint/shade math, invalid inputs, and null handling
- 135 total tests passing
- 100% line coverage overall; new resolver code covered

## Next Step

Step 1.3.4 — Build the paragraph style hierarchy: parse all `w:style` elements, link via `w:basedOn`, resolve inheritance chains

## Last Commit

Pending — step 1.3.3 implementation

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
