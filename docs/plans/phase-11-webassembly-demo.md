# Phase 11: WebAssembly Demo

**Depends on:** Phase 10 (Field Update Engine)

## Objective

Deliver `PanoramicData.Render.Demo`, a standalone WebAssembly single-page application that runs entirely in the browser and can be hosted as static assets on the repository's GitHub Pages site. A user should be able to drag a `.docx` or `.dotx` file anywhere onto the page, have it read locally with no server upload, render the document to per-page SVG, display those pages in a page-strip "sushi bar" of containers, and download the generated PDF.

The demo must handle mixed page orientations correctly. It should also use the Phase 10 field update engine so table of contents, table of figures, page numbers, and document property fields can be refreshed even when the source document has stale cached values.

## Steps

### 11.1 Browser Feasibility & Architecture

- [ ] 11.1.1 — Verify the current renderer and its dependencies can execute in browser-targeted .NET WebAssembly; document any blockers in SkiaSharp, HarfBuzz, font loading, or OpenXML package access
- [ ] 11.1.2 — If the current render path is not browser-compatible, implement the minimum browser-specific abstraction or shim layer needed without regressing existing library targets
- [ ] 11.1.3 — Add a smoke test proving a minimal DOCX can be rendered entirely client-side to SVG and PDF bytes

### 11.2 Demo Project Scaffold

- [ ] 11.2.1 — Create `PanoramicData.Render.Demo` as a standalone .NET WebAssembly SPA with no server component
- [ ] 11.2.2 — Add the project to the solution and wire in static asset publishing
- [ ] 11.2.3 — Configure base-path handling for repository-path hosting on GitHub Pages
- [ ] 11.2.4 — Configure GitHub Pages deployment to publish the built SPA to the `gh-pages` branch

### 11.3 File Intake UX

- [ ] 11.3.1 — Implement full-page drag/drop so dropping anywhere on the app accepts `.docx` and `.dotx`
- [ ] 11.3.2 — Add an accessible file-picker fallback for keyboard and touch usage
- [ ] 11.3.3 — Read the selected file locally with browser APIs; no network upload or server endpoint
- [ ] 11.3.4 — Validate file type and size and show user-friendly error states

### 11.4 Rendering Experience

- [ ] 11.4.1 — Render the uploaded document client-side using `DocxRenderer` with Phase 10 field updates enabled
- [ ] 11.4.2 — Display each page as SVG inside a scrollable page-strip or "sushi bar" of page containers
- [ ] 11.4.3 — Preserve per-page dimensions and orientation so portrait and landscape pages render correctly in the same document
- [ ] 11.4.4 — Add loading, progress, cancellation, and render-failure states
- [ ] 11.4.5 — Surface page count and basic render diagnostics in the UI

### 11.5 PDF Download & Samples

- [ ] 11.5.1 — Generate PDF bytes entirely client-side and expose a download action using the original file stem
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