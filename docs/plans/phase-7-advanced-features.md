# Phase 7: Advanced Features

**Depends on:** Phase 4 (Tables), Phase 5 (Graphics)
**Unlocks:** Phase 8 (Quality & Performance)

## Objective

Implement the remaining document features needed for high-fidelity rendering of general-purpose DOCX files. Each feature group is independent and can be implemented in any order.

## Steps

### 7.1 Fields

- [x] 7.1.1 — Parse field codes: `w:fldChar` (begin/separate/end) and `w:fldSimple` forms
- [x] 7.1.2 — Render field **results** (the cached display value between `separate` and `end`), not field **codes**
- [x] 7.1.3 — Handle `PAGE` field: compute and display the current page number
- [x] 7.1.4 — Handle `NUMPAGES` field: compute and display the total page count
- [x] 7.1.5 — Handle `DATE`, `TIME` fields: display the rendering timestamp
- [x] 7.1.6 — Handle `TOC` (Table of Contents): render the cached result text; do not recalculate
- [x] 7.1.7 — Handle `HYPERLINK` fields: emit as hyperlinks in the output
- [x] 7.1.8 — Handle `REF` / `PAGEREF` cross-references: render cached result; post-layout update page numbers if possible
- [x] 7.1.9 — Handle `IF`, `MERGEFIELD`, and other fields: render cached result with warning if result is stale
- [x] 7.1.10 — Unit tests: verify field rendering for PAGE, NUMPAGES, HYPERLINK, TOC

### 7.2 Multi-Level Lists

- [x] 7.2.1 — Resolve list numbering: abstract numbering → numbering instance → level → overrides
- [x] 7.2.2 — Compute list label text: decimal, upper/lower alpha, upper/lower roman, bullet characters, custom format strings
- [x] 7.2.3 — Handle `numFmt` patterns: `%1.`, `%1.%2.`, custom separators
- [x] 7.2.4 — Handle restart numbering: `w:lvlRestart`, `w:numRestart`
- [x] 7.2.5 — Position list labels: hanging indent with tab stop alignment to body text
- [x] 7.2.6 — Handle list continuation (paragraphs at the same list level without a number)
- [x] 7.2.7 — Handle bullet characters with specific fonts (Wingdings, Symbol)
- [x] 7.2.8 — Unit tests: verify numbering sequences, label positioning, and restart behaviour

### 7.3 Text Boxes

- [x] 7.3.1 — Parse text box elements (`w:txbxContent` inside `wsp:txbx` or VML `v:textbox`)
- [x] 7.3.2 — Lay out text box content using the text layout engine (text boxes can contain paragraphs, tables, images)
- [x] 7.3.3 — Position text box as a floating object with anchor and wrapping
- [x] 7.3.4 — Handle text box internal margins
- [x] 7.3.5 — Handle auto-size text boxes (expand to fit content)
- [x] 7.3.6 — Handle text wrapping within and around text boxes
- [x] 7.3.7 — Unit tests: verify text box positioning and content layout

### 7.4 Columns

- [x] 7.4.1 — Parse column definitions per section: count, widths, spacing, separator line
- [x] 7.4.2 — Implement column flow: text fills the first column, then overflows to the next
- [x] 7.4.3 — Handle column breaks (`<w:br w:type="column"/>`)
- [ ] 7.4.4 — Handle balanced columns (distribute content evenly, typically on the last page of a section)
- [ ] 7.4.5 — Handle unequal column widths
- [ ] 7.4.6 — Integrate column layout with floating objects: wrapping within a column
- [ ] 7.4.7 — Unit tests: verify column flow and break positions

### 7.5 Bookmarks & Hyperlinks

- [ ] 7.5.1 — Parse bookmark start/end elements (`w:bookmarkStart`, `w:bookmarkEnd`)
- [ ] 7.5.2 — Parse hyperlinks: `w:hyperlink` with external URI or internal bookmark reference
- [ ] 7.5.3 — Emit hyperlinks in SVG (`<a>`) and PDF (link annotations)
- [ ] 7.5.4 — Emit internal bookmarks in PDF as named destinations
- [ ] 7.5.5 — Unit tests: verify hyperlinks are emitted in both SVG and PDF output

### 7.6 Watermarks

- [ ] 7.6.1 — Parse watermark elements (typically a VML shape in the header with `mso-position-horizontal: center`)
- [ ] 7.6.2 — Handle text watermarks: rotated, semi-transparent text
- [ ] 7.6.3 — Handle image watermarks: centered, semi-transparent image
- [ ] 7.6.4 — Render behind all page content (z-order behind text)
- [ ] 7.6.5 — Unit tests: verify watermark rendering

### 7.7 Tab Stops (Advanced)

- [ ] 7.7.1 — Handle bar tab stops: render a vertical line at the tab position
- [ ] 7.7.2 — Handle decimal tab stops: align on the decimal point of numbers
- [ ] 7.7.3 — Handle leader characters: dot leader, hyphen leader, underscore leader, heavy leader
- [ ] 7.7.4 — Handle right-aligned tab in headers/footers (common pattern for page numbers)
- [ ] 7.7.5 — Unit tests: verify each tab stop type and leader style

### 7.8 RTL & BiDi Text

- [ ] 7.8.1 — Detect RTL paragraphs (`w:bidi`) and RTL runs (`w:rtl`)
- [ ] 7.8.2 — Apply Unicode BiDi algorithm: reorder glyph runs for mixed LTR/RTL content
- [ ] 7.8.3 — Mirror paragraph layout for RTL: right-aligned by default, indentation reversed
- [ ] 7.8.4 — Handle RTL table layout: columns ordered right-to-left
- [ ] 7.8.5 — Integrate with HarfBuzz: ensure correct shaping direction is passed to the shaper
- [ ] 7.8.6 — Unit tests: verify RTL text layout, mixed BiDi paragraphs, and RTL table layout

### 7.9 Content Controls

- [ ] 7.9.1 — Parse structured document tags (`w:sdt`)
- [ ] 7.9.2 — Render the content of the SDT (ignore the control chrome — just render the inner content)
- [ ] 7.9.3 — Handle block-level and inline-level content controls
- [ ] 7.9.4 — Unit tests: verify content controls render their inner content correctly

## Exit Criteria

- Fields render correct values (especially PAGE and NUMPAGES)
- Multi-level lists produce correct numbering sequences and label positioning
- Text boxes render with correct content, positioning, and wrapping
- Multi-column layouts flow text correctly across columns
- Hyperlinks are clickable in SVG and PDF output
- Watermarks render behind content with correct transparency and rotation
- RTL and BiDi text renders with correct reading order
- All tests pass; zero warnings

## Known Risks

- The Unicode BiDi algorithm (UAX #9) is complex. Consider using ICU4N or a well-tested .NET BiDi implementation rather than implementing from scratch.
- VML parsing (for text boxes and watermarks in older documents) is a distinct format from DrawingML; both must be supported.
- Column balancing interacts with pagination in complex ways — may require iterative layout.
- PAGE/NUMPAGES fields create a circular dependency: total page count isn't known until layout is complete, but field values affect layout. Resolve by running layout twice if the page count changes.
