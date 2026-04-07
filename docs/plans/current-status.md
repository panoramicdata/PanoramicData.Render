# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.2.5 — Integrate TeX hyphenation patterns for automatic hyphenation — **Complete**

- Created `HyphenationDictionary` implementing the Liang algorithm:
  - Parses TeX-format patterns (letters + interleaved digit levels)
  - Matches all pattern substrings against a word wrapped with `.` boundary markers
  - Computes max level at each inter-character position; odd levels allow hyphenation
  - Configurable `MinPrefix` (default 2), `MinSuffix` (default 2), `MinWordLength` (default 4)
  - Case-insensitive matching
  - `LoadPatterns(TextReader)` for bulk loading from stream (skips empty lines and `%` comments)
  - `PatternCount` property for diagnostics
- Added `EnableHyphenation` property to `RenderOptions` (default: false)
- Updated `TextRunToItemMapper` to accept optional `HyphenationDictionary` via constructor
  - When provided, `AddWordItems` calls `AddHyphenatedWordItems` for non-hyphen-terminated word parts
  - Discretionary hyphen penalties have the width of a hyphen character and are flagged
  - Explicit hyphens (U+002D) still produce their own penalties as before
- Added 22 new `HyphenationDictionaryTests` (guards, empty/short words, pattern parsing, max rule, min prefix/suffix, loading, case insensitivity, realistic words, PatternCount)
- Added 8 new mapper integration tests (with/without dictionary, discretionary flags, hyphen width, no-match words, mixed explicit+auto, multi-word)
- Added 2 new `RenderOptionsTests` assertions for `EnableHyphenation`
- 424 total tests passing, 100% line coverage maintained

## Next Step

Step 2.2.6 — Compute line break positions for a paragraph given a target line width

## Last Commit

329d045 — Implement step 2.2.4: handle non-breaking spaces and non-breaking hyphens

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
