# Current Status

## Last Updated

2026-04-06

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.4.3 (font substitution mapping) — **Complete**

- Added public `RenderOptions` API surface with defaults for:
  - `FontDirectories`
  - `FontSubstitutions`
  - `FallbackFontFamily`
  - `TargetDpi`
  - `EmbedFonts`
  - `EmbedImages`
  - `PageRange`
- Updated `FontResolver` to:
  - accept `RenderOptions`
  - honor `RenderOptions.FontSubstitutions` during lookup
  - prefer direct family matches before applying a substitution
  - treat substitution keys case-insensitively
  - leave fallback chaining for step 1.4.4
- Expanded tests:
  - `FontResolverTests` for substitution success, direct-match precedence, case-insensitive mapping, empty/default options, and invalid substitution targets
  - `RenderOptionsTests` for default values and property assignment
- 238 total tests passing
- 100% line coverage overall maintained

## Next Step

Step 1.4.4 — Implement fallback chain: requested → substitution → `FallbackFontFamily` → first available sans-serif

## Last Commit

d74e5a5 — Implement step 1.4.2: enumerate TTC faces for font metadata

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
