# Phase 11: WebAssembly Demo

**Depends on:** Phase 10 (Field Update Engine)

## Objective

Deliver `PanoramicData.Render.Demo`, a standalone WebAssembly single-page application that runs entirely in the browser and can be hosted as static assets on the repository's GitHub Pages site. A user should be able to drag a `.docx` or `.dotx` file anywhere onto the page, have it read locally with no server upload, render the document to per-page SVG, display those pages in a page-strip "sushi bar" of containers, and download the generated PDF.

The demo must handle mixed page orientations correctly. It should also use the Phase 10 field update engine so table of contents, table of figures, page numbers, and document property fields can be refreshed even when the source document has stale cached values.

## Steps

### 11.1 Browser Feasibility & Architecture

- [x] 11.1.1 — Verify the current renderer and its dependencies can execute in browser-targeted .NET WebAssembly; document any blockers in SkiaSharp, HarfBuzz, font loading, or OpenXML package access
- [x] 11.1.2 — If the current render path is not browser-compatible, implement the minimum browser-specific abstraction or shim layer needed without regressing existing library targets
- [ ] 11.1.3 — Add a smoke test proving a minimal DOCX can be rendered entirely client-side to SVG and PDF bytes

### 11.2 Demo Project Scaffold

- [x] 11.2.1 — Create `PanoramicData.Render.Demo` as a standalone .NET WebAssembly SPA with no server component
- [x] 11.2.2 — Add the project to the solution and wire in static asset publishing
- [x] 11.2.3 — Configure base-path handling for repository-path hosting on GitHub Pages
- [ ] 11.2.4 — Configure GitHub Pages deployment to publish the built SPA to the `gh-pages` branch

### 11.3 File Intake UX

- [ ] 11.3.1 — Implement full-page drag/drop so dropping anywhere on the app accepts `.docx` and `.dotx`
- [x] 11.3.2 — Add an accessible file-picker fallback for keyboard and touch usage
- [x] 11.3.3 — Read the selected file locally with browser APIs; no network upload or server endpoint
- [x] 11.3.4 — Validate file type and size and show user-friendly error states

### 11.4 Rendering Experience

- [x] 11.4.1 — Render the uploaded document client-side using `DocxRenderer` with Phase 10 field updates enabled
- [x] 11.4.2 — Display each page as SVG inside a scrollable page-strip or "sushi bar" of page containers
- [x] 11.4.3 — Preserve per-page dimensions and orientation so portrait and landscape pages render correctly in the same document
- [ ] 11.4.4 — Add loading, progress, cancellation, and render-failure states
- [ ] 11.4.5 — Surface page count and basic render diagnostics in the UI

### 11.5 PDF Download & Samples

- [x] 11.5.1 — Generate PDF bytes entirely client-side and expose a download action using the original file stem
- [ ] 11.5.2 — Optionally expose per-page SVG download for debugging if it falls out naturally from the page model
- [ ] 11.5.3 — Bundle one or more sample documents so the demo is useful before the user drops a file

### 11.6 Deployment & Validation

- [ ] 11.6.1 — Add an automated build and publish workflow for the demo site
- [ ] 11.6.2 — Validate the deployed GitHub Pages site against repository-path hosting and browser refreshes
- [ ] 11.6.3 — Add browser-based smoke coverage for drag/drop, mixed orientation pages, and PDF download
- [ ] 11.6.4 — Update README and plan docs with local run, publish, and GitHub Pages deployment instructions

## Exit Criteria

- `PanoramicData.Render.Demo` runs entirely client-side in the browser with no server upload path
- A user can drag a `.docx` or `.dotx` anywhere onto the page and see SVG output for all pages
- Portrait and landscape pages render with the correct dimensions and orientation
- The rendered PDF can be downloaded directly from the browser session
- The app is published successfully as static assets on GitHub Pages
- All tests pass; zero warnings

## Known Risks

- The current rendering stack depends on native text/layout components that may need browser-specific integration work before they can run inside WebAssembly
- Browser font availability may differ materially from desktop/server environments, which can affect fidelity
- Large documents may hit browser memory or responsiveness limits sooner than desktop renders
- GitHub Pages base-path handling can break asset loading if the SPA is not configured for repository-path hosting

---

## Renderer Fidelity Remediation Plan

Visual comparison of the `panoramic-data-document-2026.dotx` reference PDF against the rendered SVG on all three pages (Page 1: 92.1 %, Page 2: 72.3 %, Page 3: 86.3 %; average 83.6 %).

### Completed Items

| # | Issue | Pages | Status |
|---|-------|-------|--------|
| D1 | First-page header removed (`titlePg` + no First reference → null) | 1 | ✅ Done |
| D2 | NUMPAGES field shows correct page count ("3" not "1") | 2, 3 | ✅ Done |
| D3 | Table-level default cell margins (`tblCellMar`) applied from `TableNormal` | 1, 2, 3 | ✅ Done |
| D4 | Table band row colours resolve correctly through `basedOn` style chain | 1, 2, 3 | ✅ Done |
| D5 | Inline image / title overlap fixed (paragraph height includes image extent) | 1 | ✅ Done |
| D6 | Field font inheritance — computed fields (PAGE, NUMPAGES) use begin-run font/brush | 2, 3 | ✅ Done |
| D7 | Header/footer inheritance across sections (previous-section carry-forward) | 2, 3 | ✅ Done |
| D8 | Baseline offset uses font-size-based computation (not fixed 240 twips) | all | ✅ Done |
| D9 | Style cascade materializer wired into `DocxRenderer` for run + paragraph properties | all | ✅ Done |
| D10 | Body tables rendered via real table parser/layout instead of placeholder rectangle | all | ✅ Done |
| D11 | Image rendering pipeline wired end-to-end (inline + anchor/floating) | 1 | ✅ Done |
| D12 | Section-aware paragraph measurement using real content widths | all | ✅ Done |
| D13 | `Styles` element threaded through render pipeline for table-style conditional formatting | all | ✅ Done |

### Open Issues

#### A — Heading Numbering (pages 2, 3)

| # | Issue | Detail |
|---|-------|--------|
| A1 | Heading 1 number not displayed | Reference: "**1** Document control", "**2** Header 1 – short and pithy". SVG: no number prefix at all. |
| A2 | Heading 2 number missing parent level | Reference: "**1.1** Revision history", "**2.1** Header 2 – use often". SVG: "**.1** Revision history", "**.1** Header 2 – use often" — only the sub-level number appears, with a leading dot. |
| A3 | Heading 3 number not displayed | Reference: "**2.1.1** Header 3 – use sparingly". SVG: "Header 3 – use sparingly" — number missing entirely. |

**Root cause hypothesis:** `ListState.Advance()` / `ResolveListStyle()` produces labels for a single numbering level only; the multi-level concatenation logic (level 0 "1" + "." + level 1 "1" → "1.1") is not implemented. The OOXML `<w:lvlText>` format strings (e.g. `"%1.%2"`) are not being evaluated.

**Remediation:** Parse `lvlText` from the numbering definition, track counters per level, and assemble the label by substituting `%1`, `%2`, `%3` etc. with the counter values from each ancestor level. Reset child-level counters when a parent level advances.

#### B — Table Rendering (pages 1, 2, 3)

| # | Issue | Detail |
|---|-------|--------|
| B1 | Page 1 cover table wider than reference | SVG table stretches to ~70 % of page width; reference is ~50 %. The `tblW` value (or auto-fit) may not be constraining the column widths tightly enough. |
| B2 | Revision history table (p2): header text not visible | Orange header row background renders, but the white cell text ("Version", "Date", "Author(s)", "Details") is invisible. Data row ("0.1 XXXX-XX-XX …") also not visible. Possibly row-height clipping or text positioned outside clip rect. |
| B3 | Company information table (p2): missing rows | "Panoramic Data Limited" merged header, "Document Role", and "Address" rows are absent. Only "Telephone No." and "Registered in England & Wales" render. Likely caused by merged cells (`gridSpan`) or multi-line address content producing incorrect row heights. |
| B4 | Complex 4-column table (p3): entirely missing | The table with Column 1/2/3/4, Row 1/2, and numeric values does not render as a table. Cell paragraphs leak into the body text flow (fragments like "use 'Body Text' in a table to provide" appear inline). `CreateRenderableTableLayout` may be returning `null` for this table's grid. |

**Remediation plan for B1:** Investigate auto-fit vs fixed layout decision. The cover table has explicit `tblW` in the template — ensure `TableLayoutEngine` honours it and clamps column widths accordingly.

**Remediation plan for B2:** Check row-height calculation vs clip rect. The DOCX has `trHeight val="168"` for the header row. If the layout computes cell baseline below 168 twips, text is clipped. Need to ensure row height is at least `max(specified, content-needed)`.

**Remediation plan for B3:** Add merged-cell support in `TableParser.ParseCells` / `TableLayoutEngine` — `gridSpan` > 1 cells should span multiple column widths. Multi-line cell content height estimation needs to account for text wrapping within the cell's content width.

**Remediation plan for B4:** Debug why `CreateRenderableTableLayout` returns `null` for this table. Check grid-column parsing, column-width sum, and `Layout`/`LayoutAutoFit` results. The table likely has a valid grid but the layout calculation may produce zero-width columns or exceed available width.

#### C — Text Layout (pages 2, 3)

| # | Issue | Detail |
|---|-------|--------|
| C1 | Body text truncated at right margin | "This is some paragraph text. It should use the 'Body' style. A single space should separate sentences. Published d…" — text overflows/clips instead of wrapping to the next line. |
| C2 | Table cell text wrapping not working | Related to B3/B4 — cells with multi-line content (e.g. Address field, Row 1 long descriptions) do not wrap, causing incorrect heights and missing content. |

**Remediation for C1:** The body paragraph wrapping path is currently guarded to short paragraphs only. Extending the wrapping pipeline to all paragraphs (or at least increasing the token-count threshold) would fix this.

**Remediation for C2:** `TableLayoutEngine.EstimateBlockHeight` and `LayoutCellContent` need to perform full text-wrapping measurement within the cell's content width, not rely on `DefaultRowHeightTwips`.

#### D — Minor Visual Differences

| # | Issue | Detail |
|---|-------|--------|
| D-m1 | Logo position/size slightly different | Inline image horizontal centering and scaling approximately match but are not pixel-identical. Low priority. |
| D-m2 | Title / subtitle vertical position slightly different | "Title" and "[Subject]" Y-coordinates differ by a few twips, likely due to SpaceBefore/SpaceAfter or different line-height computation for large fonts. Low priority. |
| D-m3 | TOC leader dots spacing differs | Dot leaders in Table of Contents are spaced differently from reference; tab-stop resolution or dot-leader character width estimation may be slightly off. Low priority. |
| D-m4 | Bullet list sub-level markers | List Bullet 2 sub-items show open circle (○) in SVG but reference also uses ○ — alignment and indentation are slightly different. Low priority. |

### Prioritised Remediation Order

1. **A1–A3 — Heading numbering** (highest visual impact on pages 2 and 3; affects both body and TOC cross-reference accuracy)
2. **B4 — Complex table missing** (entire table absent from page 3)
3. **B3 — Company info table missing rows** (merged cells, multi-line height)
4. **B2 — Revision history header text invisible** (row-height / clipping)
5. **C1 — Body text wrapping** (paragraph wrapping threshold too low)
6. **B1 — Cover table width** (cosmetic, table slightly too wide)
7. **C2 — Table cell text wrapping** (depends on B3/B4 fixes)
8. **D-m1 to D-m4 — Minor visual polish** (low priority)