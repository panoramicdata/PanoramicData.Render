# Current Status

## Last Updated

2026-04-05

## Current Phase

Phase 1: Foundation

## Current Step

Step 1.2.7 (Footnote/endnote parsing) — **Complete**

- `NoteDefinition` record: note Id + optional note Type + parsed Blocks
- `FootnoteEndnoteParser`: parses note definitions from footnotes/endnotes parts
  - `ParseFootnotes(MainDocumentPart)`: loads and parses all `w:footnote` definitions
  - `ParseEndnotes(MainDocumentPart)`: loads and parses all `w:endnote` definitions
  - Reuses `DocumentBlockParser.CreateParagraphBlock` and `TablePlaceholderBlock` for note content
  - Handles missing parts and missing root elements as empty results
- Added 14 footnote/endnote parser tests and 6 `TestDocxBuilder` helpers for note scenarios
- 106 total tests passing
- 100% line coverage overall; new parser/model code covered

## Next Step

Step 1.3.1 — Parse `w:docDefaults` for base paragraph and run properties

## Last Commit

Pending — step 1.2.7 implementation

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
