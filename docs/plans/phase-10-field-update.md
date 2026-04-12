# Phase 10: Field Update Engine

**Depends on:** Phase 7 (Advanced Features), Phase 8 (Quality & Performance)
**Unlocks:** Phase 11 (WebAssembly SPA Demo)

## Objective

Implement a live field-update pass that replaces cached field result text (rendered as-is today) with dynamically computed values derived from the rendered layout.  This is a prerequisite for the SPA demo, where documents are rendered without having been previously open in Word, so cached values may be stale or absent.

The approach uses a **multi-pass convergence loop**: the document is laid out, field values are computed from the layout, the document model is updated in-memory, and the layout is re-run.  This repeats until all field values are stable or a configurable maximum iteration count is reached.  Convergence is typically reached in ≤ 3 passes; the worst case (TOC changes cause a page-count change that moves the TOC itself) is handled by the iteration cap.

The field updater is an **opt-in** pre-processing step — not the default code path — so existing behaviour is entirely unchanged unless the caller requests it.

## Steps

### 10.1 Design & API

- [x] 10.1.1 — Define `FieldUpdateOptions` record:
  - `UpdatePageFields` (`bool`, default `true`) — update `PAGE` and `NUMPAGES` fields
  - `UpdateDocumentProperties` (`bool`, default `true`) — update `TITLE`, `AUTHOR`, `FILENAME`, `SUBJECT`, `KEYWORDS`, `DESCRIPTION` fields
  - `UpdateTableOfContents` (`bool`, default `true`) — rebuild `TOC` / `\o` Table of Contents entries
  - `UpdateTableOfFigures` (`bool`, default `true`) — rebuild `TOC \f` Table of Figures entries
  - `MaxIterations` (`int`, default `3`) — convergence cap; must be ≥ 1
- [x] 10.1.2 — Add `FieldUpdateOptions? FieldUpdate` property to `RenderOptions` (null = feature disabled)
- [x] 10.1.3 — Add `FieldUpdateResult` record returned alongside `RenderResult`:
  - `IterationsRequired` (`int`) — how many passes were needed
  - `UpdatedFields` (`IReadOnlyList<string>`) — field types that were changed (diagnostic)
- [x] 10.1.4 — Insert the field update loop into `DocxRenderer.RenderCore` immediately before pagination when `FieldUpdateOptions` is non-null
- [x] 10.1.5 — Unit tests: verify `RenderOptions` field update properties; verify null disables the feature

### 10.2 `PAGE` and `NUMPAGES` Field Update

- [x] 10.2.1 — After first-pass layout, build a `BlockPageMap`: map from block identity to 1-based page number
- [x] 10.2.2 — Walk all `DocumentBlock` instances; for each run whose type is `FieldResult` with code `PAGE`, replace the result text with the computed page number string
- [x] 10.2.3 — Do the same for `NUMPAGES` (total page count from the layout)
- [x] 10.2.4 — Mark the block model as dirty so the next pass re-measures affected paragraphs
- [x] 10.2.5 — Unit tests: single-page document PAGE = 1; multi-page document each page correct; NUMPAGES = total

### 10.3 Document Property Fields

- [x] 10.3.1 — Load `CoreFilePropertiesPart` from the DOCX package
- [x] 10.3.2 — Build a `DocumentPropertyMap`: `{TITLE, AUTHOR, FILENAME, SUBJECT, KEYWORDS, DESCRIPTION}` → string values; use empty string for absent properties
- [x] 10.3.3 — Walk field result runs and substitute document property values
- [x] 10.3.4 — Handle `FILENAME` specially: derive from `RenderOptions.SourceFilename` if set, otherwise `"(document)"`; add `SourceFilename` property to `RenderOptions`
- [x] 10.3.5 — Unit tests: verify each document property field is updated from DOCX metadata

### 10.4 Table of Contents (`TOC`) Field Update

- [x] 10.4.1 — Identify heading paragraphs: paragraphs whose effective style is `Heading1` … `Heading9` (or mapped outline level 1–9 via `w:outlineLvl`)
- [x] 10.4.2 — Build `TocEntry` list: `{ Level, Text, PageNumber }` from the first-pass `BlockPageMap`
- [x] 10.4.3 — Locate `TOC` field containers in the document model (field begin→end spans whose code begins with `TOC`)
- [x] 10.4.4 — Determine the TOC switch set from the field code:
  - [x] `\o "1-3"` — include heading levels 1–3
  - [x] `\h` — hyperlink entries to heading bookmarks
  - [x] `\n` — no page numbers
  - [x] `\p " — "` — custom separator between label and page number
  - [x] `\t "StyleName,Level"` — include paragraphs with custom styles as TOC entries
- [x] 10.4.5 — Rebuild TOC content runs: title style + one paragraph per entry (entry text, tab leader, page number), matching the style formatting prescribed by `TOC1`–`TOC9` paragraph styles
  - [x] Materialize `TOC1`–`TOC9` paragraph/run style properties into regenerated TOC paragraphs when stale result formatting is absent
  - [x] Preserve stale-result paragraph properties, tab stops, leader dots, and template run formatting when regenerating TOC entry runs
- [x] 10.4.6 — Replace the existing field result runs in the document model with the rebuilt paragraphs
- [x] 10.4.7 — Mark the TOC containing paragraphs as reflow-required so the next layout pass re-measures them
- [x] 10.4.8 — Integration tests: render a document with a TOC; verify TOC entries and page numbers match headings

### 10.5 Table of Figures (`TOF`) Field Update

- [x] 10.5.1 — Identify figure-caption paragraphs: paragraphs whose style is `Caption` or whose content begins with the `SEQ Figure` sequence field
- [x] 10.5.2 — Build `TofEntry` list: `{ SequenceNumber, Text, PageNumber }` from the `BlockPageMap`
- [x] 10.5.3 — Locate `TOC \f` field containers in the document model
- [x] 10.5.4 — Rebuild TOF content runs and replace in document model (analogous to 10.4.5–10.4.6)
- [x] 10.5.5 — Integration tests: render a document with a Table of Figures field; verify entries and page numbers are correct

### 10.6 Multi-Pass Convergence Loop

- [x] 10.6.1 — After each field update pass, compare old vs new field result text for every updated field
- [x] 10.6.2 — If all field values are identical to the previous pass, stop (converged)
- [x] 10.6.3 — If any value changed AND iterations remaining > 0, re-run layout and repeat from step 10.2
- [x] 10.6.4 — If max iterations reached without convergence, emit a `LogWarning` and use the last computed values
- [x] 10.6.5 — Record `IterationsRequired` in `FieldUpdateResult`
- [x] 10.6.6 — Integration tests:
  - [x] Document with a TOC that fits on one page: verify 1–2 iterations
  - [x] Document where TOC addition pushes text to a new page: verify convergence within 3 iterations

### 10.7 SEQ Field Update (Sequence Numbers)

- [x] 10.7.1 — Walk all `SEQ` fields in document order and assign sequential counter values per identifier
- [x] 10.7.2 — Handle `\r N` (reset) and `\h` (hidden) switches
- [x] 10.7.3 — Update figure/table/equation caption numbering
- [x] 10.7.4 — Unit tests: verify sequential numbering per sequence identifier; verify reset switch

### 10.8 PAGEREF / REF Cross-Reference Update

- [x] 10.8.1 — After first-pass layout, build a `BookmarkPageMap`: bookmark name → page number
- [x] 10.8.2 — Walk `PAGEREF` field result runs and replace with the page number of the target bookmark
- [x] 10.8.3 — Walk `REF` field result runs and replace with the text of the target bookmark
- [x] 10.8.4 — Unit tests: verify PAGEREF resolves to the correct page; verify REF resolves to bookmark text

### 10.9 Testing & Documentation

- [x] 10.9.1 — Add `with-toc.docx` to the test corpus: a document with a deliberate three-level TOC containing stale page numbers; verify the update engine produces correct page numbers
- [x] 10.9.2 — Add `with-tof.docx` to the test corpus: a document with a Table of Figures with stale sequence numbers and page numbers
- [x] 10.9.3 — Add `with-cross-refs.docx` to the test corpus: PAGEREF and REF fields
- [x] 10.9.4 — Add corpus-level field-update verification tests (`CorpusWithToc`, `CorpusWithTof`, `CorpusWithCrossRefs`)

### 10.10 Word Interop Field-Update Corpus (Visual Regression)

Generate a set of documents where fields are **deliberately stale** and the reference PNGs show Word's own field-updated rendering — proving our field update engine matches Word's output.

Workflow per document:
1. Use Word Interop to create a seed document with valid fields (e.g., a 1-heading TOC)
2. Save via Word so the initial field values are correct
3. Re-open via OpenXML SDK, add extra content (many headings, captions, pages) WITHOUT updating fields → save as the stale source DOCX
4. Open the stale DOCX in Word, call `doc.Fields.Update()`, export as PDF → PNG for the reference

Documents:
- [x] 10.10.1 — `field-update-toc`: seed has 1 heading + TOC; stale source adds 10+ headings across multiple pages; reference shows full TOC with correct page numbers
- [x] 10.10.2 — `field-update-tof`: seed has 1 caption + TOF; stale source adds 5+ Caption-style paragraphs; reference shows full Table of Figures
- [x] 10.10.3 — `field-update-page-of`: seed has a "Page X of Y" footer (PAGE + NUMPAGES fields); stale source adds enough content to span 5+ pages; reference shows correct "Page N of 5" per page
- [x] 10.10.4 — `field-update-cross-refs`: seed has bookmarked content + PAGEREF/REF fields on page 1; stale source adds content between fields and bookmark to push bookmark to a later page; reference shows resolved page numbers and text

Implementation:
- [x] 10.10.5 — Add `generate-field-update-corpus` command to the reference generator CLI
- [x] 10.10.6 — Implement `FieldUpdateCorpusGenerator` class with Word Interop seed creation, OpenXML staleness injection, and Word Interop reference rendering
- [x] 10.10.7 — Add thresholds and visual regression entries for the new field-update documents
- [x] 10.10.8 — Add integration tests that render stale DOCX with `FieldUpdateOptions` enabled and verify SVG output matches expected content

### 10.11 Documentation

- [x] 10.11.1 — Update DESIGN.md with field update architecture and convergence model
- [x] 10.11.2 — Update XML documentation on all new public/internal types

## Exit Criteria

- `PAGE`, `NUMPAGES`, document properties, TOC, TOF, SEQ, PAGEREF, and REF fields are all updated correctly when `FieldUpdateOptions` is non-null
- Convergence loop terminates within `MaxIterations` for all corpus documents
- No regression in any existing test when `FieldUpdateOptions` is null (the default)
- New corpus documents pass visual regression against Word-rendered references
- Field-update corpus documents (stale source → our engine) produce visual output matching Word's own field-updated references
- All tests pass; zero warnings; 100% line coverage

## Known Risks

- **TOC style fidelity**: Rebuilding TOC paragraphs requires matching Word's TOC1–TOC9 styles exactly; any deviation will show up as a visual regression.
- **Complex TOC switches**: The full set of TOC switches (`\b`, `\d`, `\e`, `\l`, `\s`, `\w`, `\x`, `\z`) is large; only the most common subset is in scope for this phase.
- **Non-converging documents**: Theoretically possible (TOC expands, then shrinks, then expands…); the iteration cap prevents infinite loops.
- **Performance**: Each additional pass adds the full layout cost.  Measure and ensure total time with `MaxIterations = 3` is still within Phase 8 performance targets.
