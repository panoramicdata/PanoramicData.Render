# Phase 3: Page Layout

**Depends on:** Phase 2 (Text Layout)
**Unlocks:** Phase 4 (Tables), Phase 5 (Graphics)

## Objective

Implement the pagination engine that splits a continuous text flow into discrete pages, respecting page dimensions, margins, section breaks, header/footer regions, and footnote/endnote placement.

## Steps

### 3.1 Basic Pagination

- [x] 3.1.1 — Implement `PageBuilder`: given a stream of laid-out blocks (from Phase 2), split into pages based on page height minus top/bottom margins
- [x] 3.1.2 — Handle paragraph splitting: when a paragraph straddles a page boundary, split at a line boundary
- [x] 3.1.3 — Handle explicit page breaks: `<w:br w:type="page"/>` and `<w:lastRenderedPageBreak/>`
- [x] 3.1.4 — Handle `pageBreakBefore` paragraph property (paragraph starts on a new page)
- [x] 3.1.5 — Implement widow/orphan control: ensure at least N lines remain at the top/bottom of a page (Word default: 2)
- [x] 3.1.6 — Implement `keepNext`: a paragraph with `keepNext` must appear on the same page as the following paragraph
- [x] 3.1.7 — Implement `keepLines`: all lines of a paragraph must appear on the same page
- [x] 3.1.8 — Unit tests: verify page break positions for documents with known pagination

### 3.2 Sections

- [x] 3.2.1 — Handle section breaks: next page, continuous, odd page, even page
- [x] 3.2.2 — Apply per-section page dimensions: page width, height, orientation (portrait/landscape)
- [x] 3.2.3 — Apply per-section margins: top, bottom, left, right, gutter
- [x] 3.2.4 — Handle continuous section breaks with different column counts (defer column layout to Phase 7, but track the section boundary)
- [x] 3.2.5 — Handle line numbering properties per section (if present; rendering is best-effort)
- [x] 3.2.6 — Unit tests: verify multi-section documents produce correct page sizes and break positions

### 3.3 Headers & Footers

- [x] 3.3.1 — Resolve which header/footer applies to each page: default, first-page, odd/even, per-section
- [x] 3.3.2 — Lay out header content using the text layout engine (headers can contain tables, images, etc.)
- [x] 3.3.3 — Lay out footer content similarly
- [x] 3.3.4 — Reserve header/footer height from the page's available content area
- [x] 3.3.5 — Position header within top margin area; position footer within bottom margin area
- [x] 3.3.6 — Handle `w:headerDistance` and `w:footerDistance` (distance from page edge)
- [x] 3.3.7 — Unit tests: verify correct header/footer selection and positioning

### 3.4 Footnotes & Endnotes

- [x] 3.4.1 — Implement footnote reference markers: superscript numbering in body text
- [x] 3.4.2 — Lay out footnote content at the bottom of the page (above the footer)
- [x] 3.4.3 — Reserve space for footnotes: compute footnote height before finalizing page break positions
- [x] 3.4.4 — Handle footnotes that exceed remaining page space: continue footnote on the next page
- [x] 3.4.5 — Implement footnote separator line
- [ ] 3.4.6 — Implement endnotes: collected and rendered at the end of the document or section (per settings)
- [ ] 3.4.7 — Unit tests: verify footnote placement and page flow impact

## Exit Criteria

- Multi-section documents paginate correctly with different page sizes and orientations
- Headers and footers render in the correct position with correct content per page
- Footnotes appear at the bottom of the correct page and affect pagination
- Widow/orphan, keepNext, and keepLines controls are respected
- All tests pass; zero warnings

## Known Risks

- Footnotes that span multiple pages create a circular dependency: the footnote affects pagination, which affects where the footnote reference lands. Word resolves this iteratively; we may need multiple layout passes.
- Headers/footers can contain complex content (tables, images) which requires the full layout engine — ensure the layout engine is re-entrant.
- Continuous section breaks with different column counts are deferred to Phase 7 but must not break pagination here.
