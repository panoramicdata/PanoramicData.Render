# Current Status

## Last Updated

2026-04-07

## Current Phase

Phase 2: Text Layout

## Current Step

Step 2.2.4 — Handle non-breaking spaces and non-breaking hyphens — **Complete**

- Non-breaking space (U+00A0) already excluded from `IsSpace()` — treated as word character, no break opportunity
  - Added explicit documentation comment on `IsSpace` to clarify intent
  - Three tests verify: single box for U+00A0-joined text, no glue produced, correct interaction with regular spaces
- Non-breaking hyphen (U+2011) already excluded from `SplitOnHyphens()` (only splits on U+002D)
  - Two tests verify: no penalty produced, contrasted behavior vs regular hyphen
- Created `NonBreakingHyphenRunElement` marker class for `<w:noBreakHyphen/>` OpenXML elements
- Updated `RunElementParser` to handle `NoBreakHyphen` → `NonBreakingHyphenRunElement`
- Updated `TextRunToItemMapper.MapRunElements` to handle `NonBreakingHyphenRunElement` → box with hyphen-character width
- Added 10 new tests (3 non-breaking space, 2 non-breaking hyphen U+2011, 3 NonBreakingHyphenRunElement, 2 parser)
- 394 total tests passing, 100% line coverage maintained

## Next Step

Step 2.2.5 — Optional: integrate TeX hyphenation patterns for automatic hyphenation

## Last Commit

e180d5f — Implement step 2.2.3: handle forced breaks (line, page, column)

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
