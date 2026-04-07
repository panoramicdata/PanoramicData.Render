# Phase 2: Text Layout

**Depends on:** Phase 1 (Foundation)
**Unlocks:** Phase 3 (Page Layout), Phase 6 (Output Drivers)

## Objective

Build the measurement engine, implement Knuth-Plass line breaking, and handle all paragraph and character formatting to produce correctly positioned text within a single infinite-height column.

At the end of this phase, a simple document's text can be laid out into lines with correct positions — but without pagination, headers, or complex elements.

## Steps

### 2.1 Measurement Engine

- [x] 2.1.1 — Create `MeasurementEngine` class wrapping SkiaSharp: given a `SKTypeface`, font size, and text string, return glyph advance widths
- [x] 2.1.2 — Integrate HarfBuzz shaping via `SKShaper`: produce shaped glyph runs with correct advance widths, kerning, and ligatures
- [x] 2.1.3 — Handle measurement in twips: all measurements returned in twips; conversion to output units deferred to render time
- [x] 2.1.4 — Measure individual characters for superscript/subscript offset calculations
- [x] 2.1.5 — Unit tests: verify measurements for known fonts produce expected widths (tolerance: ±1 twip)

### 2.2 Knuth-Plass Line Breaking

- [x] 2.2.1 — Implement the Knuth-Plass optimal paragraph-breaking algorithm: box, glue, penalty model
- [x] 2.2.2 — Map text runs to Knuth-Plass items: words → boxes, spaces → glue (with stretch/shrink), hyphens → penalties
- [x] 2.2.3 — Handle forced breaks: `<w:br/>` (line break), `<w:br w:type="page"/>` (page break), `<w:br w:type="column"/>` (column break)
- [x] 2.2.4 — Handle non-breaking spaces and non-breaking hyphens
- [x] 2.2.5 — Optional: integrate TeX hyphenation patterns for automatic hyphenation (controlled by `RenderOptions.EnableHyphenation`)
- [x] 2.2.6 — Compute line break positions for a paragraph given a target line width
- [x] 2.2.7 — Unit tests: verify break positions against hand-computed expected results for at least 10 paragraphs of varying complexity

### 2.3 Paragraph Formatting

- [x] 2.3.1 — **Alignment:** Left, Right, Center, Justified — compute X offsets for each glyph run per line
- [x] 2.3.2 — **Justification:** Distribute extra whitespace across glue items on justified lines; do not justify the last line of a paragraph
- [x] 2.3.3 — **Indentation:** First-line indent, hanging indent, left margin, right margin
- [x] 2.3.4 — **Spacing:** Space before/after paragraph (in twips), line spacing (single, 1.5, double, exact, at-least, multiple)
- [x] 2.3.5 — **Tab stops:** Left, center, right, decimal, bar tab stops; leader characters (dot, hyphen, underscore)
- [x] 2.3.6 — **Default tab stops:** Use document settings' default tab stop interval when no explicit tab stops defined
- [x] 2.3.7 — **Borders and shading:** Paragraph borders (top, bottom, left, right, between), paragraph background color
- [x] 2.3.8 — Unit tests: verify paragraph metrics (total height, line positions, indentation offsets) for each formatting type

### 2.4 Character Formatting

- [x] 2.4.1 — **Font properties:** Family, size, bold, italic — select correct `SKTypeface`
- [x] 2.4.2 — **Decorations:** Underline (single, double, thick, dotted, dashed, wavy, etc.), strikethrough, double-strikethrough
- [x] 2.4.3 — **Color:** Foreground color, resolved from theme color + tint/shade or explicit RGB
- [ ] 2.4.4 — **Highlight:** Background highlight (the 16 named Word highlight colors)
- [ ] 2.4.5 — **Superscript / Subscript:** Adjust baseline offset and font size (typically 2/3 of parent size)
- [ ] 2.4.6 — **Small Caps / All Caps:** Transform text and adjust sizing for small caps
- [ ] 2.4.7 — **Character spacing:** Expanded/condensed spacing (`w:spacing` on `w:rPr`)
- [ ] 2.4.8 — **Vanish (hidden text):** Exclude from layout when hidden text is not displayed
- [ ] 2.4.9 — Unit tests: verify each formatting property produces correct render instructions

## Exit Criteria

- A DOCX file containing only formatted text (no tables, images, headers) produces a complete set of positioned glyph runs
- Line breaks match expected positions within ±2 glyph widths of Word's output for test documents
- All paragraph alignment modes produce correct horizontal positioning
- Tab stops position text correctly
- All character formatting properties produce correct render instructions
- All tests pass; zero warnings

## Known Risks

- Knuth-Plass implementation is well-documented but parameter tuning (`tolerance`, `demerits` weights) will require iteration against Word's output.
- Tab stop layout (especially decimal tabs) interacts with font metrics in surprising ways.
- HarfBuzz shaping for complex scripts may produce different cluster/glyph counts than expected; careful handling of shaped glyph positions is needed.
