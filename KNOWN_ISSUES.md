# Known Issues

This document tracks confirmed bugs and issues that are not yet fixed. Each issue should be tracked as a GitHub Issue with the `status/known-bug` label until resolved.

## Visual Regression Test Failures

### Issue #1: inline-images.docx cannot be opened by Word COM
**GitHub Issue:** [#14](https://github.com/panoramicdata/PanoramicData.Render/issues/14)
**Status:** FIXED ✅
**Component:** ReferenceGenerator, TestCorpusGenerator
**Severity:** Medium (was blocking baseline generation)
**Root Cause:** Inline drawing markup was generated with insufficient non-visual frame metadata for Word COM.
**Fix:** Updated image drawing generation to include explicit `GraphicFrameLocks` and removed empty effect extents in inline image markup.
**Verification:** `ReferenceGenerator render` now exports `inline-images.docx` successfully and produces reference PNGs.
**Resolution:** Removed from known-missing-reference set in visual regression tests.

### Issue #2: floating-images.docx cannot be opened by Word COM
**GitHub Issue:** [#15](https://github.com/panoramicdata/PanoramicData.Render/issues/15)
**Status:** FIXED ✅
**Component:** ReferenceGenerator, TestCorpusGenerator
**Severity:** Medium (was blocking baseline generation)
**Root Cause:** Floating-anchor drawing markup was generated with incomplete non-visual frame metadata for Word COM.
**Fix:** Updated anchored image drawing generation to include explicit `GraphicFrameLocks` and removed empty effect extents in anchor markup.
**Verification:** `ReferenceGenerator render` now exports `floating-images.docx` successfully and produces reference PNGs.
**Resolution:** Removed from known-missing-reference set in visual regression tests.

### Issue #3: page-break.docx renders differently than Word baseline
**Status:** FIXED ✅
**Component:** DocxRenderer, Pagination
**Severity:** High (affects page-break feature correctness)
**Root Cause:** RESOLVED - Pagination logic was not handling Break elements with Type="Page" inside run streams.
- Fixed in DocumentBlockParser by detecting page/column break elements within paragraph runs
- Now correctly converts these to ForcePageBreakBefore markers on subsequent paragraphs
**Solution:** Modified DocumentBlockParser to:
- Detect Break elements with RunBreakType.Page in run streams
- Apply PageBreakBefore property to paragraphs containing page breaks
**Verification:**
- page-break.docx now renders as 3 pages (correct, matching Word baseline)
- Previously rendered as 1 page (incorrect)
**Status:** Ready to remove from KnownPageCountMismatchDocuments after reference PNG validation

### Issue #4: panoramic-data-document-2026.dotx renders differently than Word baseline
**GitHub Issue:** [#16](https://github.com/panoramicdata/PanoramicData.Render/issues/16)
**Status:** FIXED ✅
**Component:** DocxRenderer, Pagination, DOTX Support
**Severity:** Medium (was affecting real-world template rendering)
**Root Cause:** Baseline pagination underestimated vertical flow for realistic template content under default line-height assumptions.
**Fix:** Increased default natural line-height baseline used for paragraph measurement in `DocumentLayoutEngine` from 240 twips to 360 twips.
**Verification:** Focused regression tests now match reference page counts for `inline-images`, `floating-images`, and `panoramic-data-document-2026`.
**Resolution:** Removed page-count known-gap exception in visual regression tests.

## Development Policy

Per the .github/copilot-instructions.md Test Policy:
- **NEVER skip, suppress, or work around failing tests**
- All failing tests must have a corresponding GitHub Issue
- Issues should include:
  - Clear description of root cause
  - Reproduction steps
  - Expected vs actual behavior
- Known gaps are only accepted temporarily:
  - Issue must be created and tracked
  - Must have `status/known-bug` label
  - Must reference the GitHub Issue number in code comments
  - Must be removed immediately when fixed

## See Also

- VisualRegressionComparisonTests.cs - Test with known-gap handling
- TestCorpusGenerator.cs - Generates test documents
- PanoramicData.Render.ReferenceGenerator - Generates Word baselines via COM Interop

