# Phase 4: Tables

**Depends on:** Phase 3 (Page Layout)
**Unlocks:** Phase 7 (Advanced Features)

## Objective

Implement table layout including fixed and auto-fit algorithms, cell merging, border rendering, and nested tables. Tables are among the most complex layout elements in OOXML and will require significant effort.

## Steps

### 4.1 Table Model

- [x] 4.1.1 — Parse table structure from OpenXML: `w:tbl`, `w:tr`, `w:tc`, grid columns (`w:tblGrid`)
- [x] 4.1.2 — Parse table properties: width (fixed, percentage, auto), alignment (left, center, right), indentation
- [x] 4.1.3 — Parse row properties: height (exact, at-least, auto), header row (repeat on each page), `cantSplit`
- [x] 4.1.4 — Parse cell properties: width, vertical alignment (top, center, bottom), text direction, margins/padding
- [x] 4.1.5 — Parse cell merging: horizontal merge (`w:gridSpan`), vertical merge (`w:vMerge` start/continue)
- [x] 4.1.6 — Unit tests: verify table model correctly represents all test documents

### 4.2 Fixed Layout Tables

- [x] 4.2.1 — Implement fixed-width table layout: column widths are specified explicitly in the grid
- [x] 4.2.2 — Compute cell positions: $(x, y)$ for each cell based on grid column offsets and accumulated row heights
- [x] 4.2.3 — Lay out cell content using the text layout engine (cells contain paragraphs, possibly nested tables)
- [x] 4.2.4 — Handle cell margins (top, bottom, left, right padding)
- [x] 4.2.5 — Handle vertical alignment within cells (top, center, bottom)
- [x] 4.2.6 — Unit tests: verify cell positions and content layout

### 4.3 Auto-Fit Tables

- [x] 4.3.1 — Implement the auto-fit algorithm: measure preferred/minimum/maximum widths for each column
- [x] 4.3.2 — Preferred width: measure cell content at unlimited width to get natural width
- [x] 4.3.3 — Minimum width: measure the widest non-breakable unit (word or image) per cell
- [x] 4.3.4 — Distribute available table width across columns proportionally to preferred widths, respecting minimums
- [x] 4.3.5 — Handle percentage-based column widths
- [x] 4.3.6 — Handle mixed fixed + auto columns
- [x] 4.3.7 — Re-lay out cell content at final computed column widths
- [x] 4.3.8 — Unit tests: verify auto-fit produces reasonable column widths for various content patterns

### 4.4 Cell Merging

- [x] 4.4.1 — Handle horizontal merges: cell spans multiple grid columns (`w:gridSpan`)
- [ ] 4.4.2 — Handle vertical merges: cell spans multiple rows (`w:vMerge`)
- [ ] 4.4.3 — Combine horizontal + vertical merges: a single cell spanning a rectangular region
- [ ] 4.4.4 — Adjust content layout area and position for merged cells
- [ ] 4.4.5 — Unit tests: verify merged cell regions and content positioning

### 4.5 Table Borders

- [ ] 4.5.1 — Parse border definitions: width, color, dash style (single, double, dotted, dashed, thick, etc.)
- [ ] 4.5.2 — Resolve conflict between table-level, row-level, and cell-level borders (highest priority wins per the OOXML spec)
- [ ] 4.5.3 — Handle `insideH` and `insideV` borders (internal grid lines)
- [ ] 4.5.4 — Handle border spacing (distance between border and cell content)
- [ ] 4.5.5 — Render borders as line segments with appropriate width, color, and dash pattern
- [ ] 4.5.6 — Unit tests: verify border resolution and rendering for complex border scenarios

### 4.6 Table Pagination

- [ ] 4.6.1 — Handle table rows that span page boundaries: split row at a cell content line boundary (if `cantSplit` is not set)
- [ ] 4.6.2 — Handle `cantSplit` rows: move the entire row to the next page
- [ ] 4.6.3 — Handle header rows: repeat on each page when the table spans multiple pages
- [ ] 4.6.4 — Unit tests: verify table pagination for multi-page tables

### 4.7 Nested Tables

- [ ] 4.7.1 — Handle tables inside table cells: recursive layout
- [ ] 4.7.2 — Ensure auto-fit width calculation accounts for nested table constraints
- [ ] 4.7.3 — Unit tests: verify nested table layout

### 4.8 Table Cell Shading

- [ ] 4.8.1 — Parse cell shading: fill color, pattern (clear, solid, horizontal stripe, etc.)
- [ ] 4.8.2 — Render cell backgrounds before cell content
- [ ] 4.8.3 — Handle table style conditional formatting (banded rows, banded columns, first/last row/column)
- [ ] 4.8.4 — Unit tests: verify cell shading and conditional formatting

## Exit Criteria

- Fixed-width tables render with correct cell positioning and content layout
- Auto-fit tables produce reasonable column widths that match Word's output closely
- Merged cells (horizontal, vertical, combined) render correctly
- Borders resolve correctly including conflict resolution
- Tables paginate correctly with header row repetition
- Nested tables render correctly
- All tests pass; zero warnings

## Known Risks

- Auto-fit table layout is notoriously under-specified in the OOXML standard. Word's algorithm is undocumented and was reverse-engineered by the LibreOffice team over years. Expect iterative refinement against real documents.
- Table row splitting (when a row straddles a page boundary) is complex: each cell must be split independently, and the split point depends on content in all cells of the row.
- Nested tables with auto-fit create recursive constraint satisfaction problems.
