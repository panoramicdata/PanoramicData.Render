# Current Status

## Last Updated

2026-04-12

## Current Phase

Phase 10: Field Update Engine — **IN PROGRESS**

## Current Step

Steps 10.1.4, 10.2, 10.3, and the first 10.4 TOC slice — **COMPLETE**

- Added `FieldUpdateOptions` with default-on switches for page fields, document properties, TOC, TOF, and a validated `MaxIterations` default of 3.
- Added `RenderOptions.FieldUpdate` as an opt-in nullable property, leaving existing rendering behaviour unchanged when unset.
- Added `FieldUpdateResult` and exposed nullable `RenderResult.FieldUpdateResult` metadata for later convergence-loop diagnostics.
- Added unit coverage proving the new options default correctly, reject invalid `MaxIterations`, and remain null/inactive in the default render path.
- Current implementation intent: wire the first `FieldUpdateOptions` execution path into `DocxRenderer.RenderCore` and implement the initial `PAGE` / `NUMPAGES` update pass with focused regression tests.
- Implemented the opt-in field-update loop in `DocxRenderer.RenderCore`, including iteration counting and `FieldUpdateResult` population.
- Added `FieldUpdateEngine` to build a block-to-page map from `LayoutPage.Blocks` and rewrite cached complex/simple `PAGE` and `NUMPAGES` field results in paragraph XML.
- Added focused tests covering in-memory cached-result mutation and the public render path returning non-null field-update metadata when enabled.
- Extended `FieldUpdateEngine` to update cached `TITLE`, `AUTHOR`, `SUBJECT`, `KEYWORDS`, `DESCRIPTION`, and `FILENAME` fields from DOCX core properties plus `RenderOptions.SourceFilename`.
- Added focused tests proving document property field values are rewritten in-memory and rendered into SVG output through the public `DocxRenderer` path.
- Added a first TOC update slice that identifies headings from paragraph style/outline level, builds `TocEntry` records from the page map, finds `TOC` field containers, and replaces following `TOC1` paragraphs with regenerated entries.
- `DocxRenderer.RenderCore` now reparses `DocumentBlockParser.Parse(doc.DocumentBody)` on each field-update rerun, because TOC rebuilds are structural and can add/remove paragraphs before remeasurement.
- Added focused TOC coverage for both the internal updater and the public render path; the first-page SVG now renders regenerated TOC entries instead of stale cached text.
- TOC switch handling now covers the common switch subset: `\o`, `\h`, `\n`, `\p`, and `\t` are wired and covered by focused tests.
- `\h` now rebuilds TOC entries as internal bookmark hyperlinks by wrapping generated entry text in `w:hyperlink` elements that target heading bookmark starts, and the public SVG output emits matching `xlink:href="#bookmark"` anchors.
- When a heading included in a `\h` TOC has no bookmark, `FieldUpdateEngine` now injects a synthetic `_TocGenerated*` bookmark into the heading paragraph and targets that generated bookmark from the rebuilt TOC entry.

## Next Step

Continue step 10.4 table of contents work:

- finish the remaining TOC switch subset, especially `\h` hyperlinks and `\t` custom-style mappings
- improve TOC output fidelity beyond plain regenerated `TOC1` paragraphs
- decide whether TOF can reuse the same structural paragraph-rebuild path
- decide whether synthetic TOC bookmark generation should remain an internal implementation detail or be documented in DESIGN.md for Phase 10

## Last Commit

Resolve image/reference issue set and close issue regressions (commit 1aa469f)

Relevant recent validations completed successfully:

- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~Apply_TableOfContents_WithHyperlinkSwitch_GeneratesSyntheticBookmarksForHeadingsWithoutAnchors|FullyQualifiedName~Render_WithHyperlinkedTableOfContentsFieldUpdateAndNoHeadingBookmarks_EmitsSyntheticBookmarkLinksInSvg"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~Apply_TableOfContents_WithHyperlinkSwitch_WrapsEntriesInBookmarkHyperlinks|FullyQualifiedName~Render_WithHyperlinkedTableOfContentsFieldUpdate_EmitsBookmarkLinksInSvg"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~Apply_TableOfContents_WithCustomStyleSwitch_IncludesMappedParagraphs|FullyQualifiedName~Render_WithCustomStyleTableOfContentsFieldUpdate_RendersMappedEntriesOnFirstPage"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~Apply_TableOfContents_WithCustomSeparator_OmitsDefaultTabLeader|FullyQualifiedName~Apply_TableOfContents_WithNoPageNumbersSwitch_OmitsPageNumbers|FullyQualifiedName~Apply_TableOfContents_WithCustomStyleSwitch_IncludesMappedParagraphs"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~Apply_TableOfContents_RebuildsEntryParagraphs|FullyQualifiedName~Render_WithTableOfContentsFieldUpdate_RendersGeneratedEntriesOnFirstPage"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~Apply_DocumentPropertyFields_UpdatesCachedResultText|FullyQualifiedName~Render_WithDocumentPropertyFieldUpdate_RendersUpdatedPropertyValues|FullyQualifiedName~RenderOptionsTests"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~RenderOptionsTests|FullyQualifiedName~DocxRendererTests|FullyQualifiedName~FieldUpdateEngineTests"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~FieldUpdateEngineTests|FullyQualifiedName~Render_WithFieldUpdateEnabled_ReturnsFieldUpdateResult"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~RenderOptionsTests|FullyQualifiedName~DocxRendererTests"`
- `dotnet build -c Release`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --filter "FullyQualifiedName~CorpusPageCountRegressionTests"`
- `dotnet test PanoramicData.Render.Test/PanoramicData.Render.Test.csproj -c Release --no-build --filter "FullyQualifiedName~VisualRegressionComparisonTests"`

## Blockers

- Browser/WebAssembly compatibility for the current native text/rendering stack still needs to be proven. This is an explicit Phase 11 task, not a blocker for starting Phase 10.

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
- Font embedding via TTF data URIs (pragmatic MVP; WOFF2 upgrade deferred pending library availability)
- Remaining active roadmap is intentionally narrowed to Phase 10 (field updates) and Phase 11 (WebAssembly demo)
- Remaining unfinished non-SPA work is tracked as GitHub backlog issues instead of open phase checklist items
- GitHub Pages deployment should assume a `gh-pages` branch unless repository settings require an Actions-based Pages publish flow instead
- Phase 10 is being implemented as an API-first, test-first series of narrow slices so the default render path stays stable while field recalculation is introduced incrementally
- Existing render-time PAGE and NUMPAGES substitution remains in place; the new field-update loop now also rewrites cached body-field results to support the broader multi-pass field-update architecture
- Document property fields are updated from package core properties via `DocxDocument` accessors, with `RenderOptions.SourceFilename` supplying the browser-upload filename for `FILENAME` fields
- Structural TOC updates require reparsing the body blocks between field-update iterations so inserted/replaced TOC paragraphs participate in measurement and pagination
- TOC `\t` custom-style mappings resolve against the paragraph's direct style ID and the style's human-readable name from the styles part
- TOC `\h` hyperlinks currently target the first preferred bookmark on a heading paragraph, preferring `_Toc*` bookmark names when present and otherwise falling back to the first named bookmark
- When no suitable heading bookmark exists for `\h`, the updater injects a synthetic `_TocGenerated{n}` bookmark and treats that injection as a structural TOC change so the next layout pass emits the named destination
