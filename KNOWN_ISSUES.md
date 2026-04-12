# Known Issues

This document tracks confirmed bugs and issues that are not yet fixed. Each issue should be tracked as a GitHub Issue with the `status/known-bug` label until resolved.

## Visual Regression Test Failures

### Issue #1: inline-images.docx cannot be opened by Word COM
**Status:** Needs Investigation
**Component:** ReferenceGenerator, TestCorpusGenerator
**Severity:** Medium (blocks baseline generation for one test document)
**Root Cause:** Word COM Interop fails to open inline-images.docx with error: "Word experienced an error trying to open the file."
- The DOCX file is valid according to OpenXML SDK validation
- Word COM cannot open it despite proper OpenXML structure
- Alternative tools (LibreOffice, etc.) may open it successfully
**Impact:** 
- Visual regression test cannot generate Word baseline PNG for inline-images
- Test is skipped with a known gap marker
**Reproduction:** Run `PanoramicData.Render.ReferenceGenerator render test-assets/docx test-assets/reference`
**Next Steps:**
1. Compare inline-images.docx XML structure with a Word-generated inline image document
2. Check if namespace declarations or element ordering needs adjustment
3. Verify image part relationships are correct
4. Consider using Word COM to generate the test document instead of OpenXML SDK

### Issue #2: floating-images.docx cannot be opened by Word COM
**Status:** Needs Investigation
**Component:** ReferenceGenerator, TestCorpusGenerator
**Severity:** Medium (blocks baseline generation for one test document)
**Root Cause:** Word COM Interop fails to open floating-images.docx with error: "Word experienced an error trying to open the file."
- The DOCX file is valid according to OpenXML SDK validation
- Word COM cannot open it despite proper OpenXML structure
- Similar to Issue #1 but for floating (anchored) images instead of inline images
**Impact:**
- Visual regression test cannot generate Word baseline PNG for floating-images
- Test is skipped with a known gap marker
**Reproduction:** Run `PanoramicData.Render.ReferenceGenerator render test-assets/docx test-assets/reference`
**Next Steps:**
1. Compare floating-images.docx XML structure with a Word-generated floating image document
2. Check if Anchor element structure or namespace declarations need adjustment
3. Verify image part relationships and wrapping properties
4. Consider using Word COM to generate the test document instead of OpenXML SDK

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
**Status:** Needs Investigation
**Component:** DocxRenderer, Pagination, DOTX Support
**Severity:** Medium (affects real-world template rendering)
**Root Cause:** Pagination logic calculates layout differently for this complex template
- DocxRenderer renders panoramic-data-document-2026.dotx as 2 pages
- Word baseline has 3 pages
- Complex template with styles, headers/footers, and formatting may expose pagination bugs
- May be related to Issue #3 (page-break pagination) or separate style-handling issue
**Impact:**
- Test accepts this as a known page count mismatch
- Real-world template is not rendering correctly
- Indicates potential interaction between styles, pagination, and complex formatting
**Expected Behavior:**
- Should render exactly as Word baseline (3 pages)
**Reproduction:**
1. Run `dotnet test ... VisualRegressionComparisonTests`
2. Observe: panoramic-data-document-2026 → Rendered=2, Reference=3
**Next Steps:**
1. Extract the missing page content from Word baseline (page 3)
2. Debug if content is being skipped, merged, or hidden
3. Check style cascade and conditional formatting application
4. Review header/footer and section break handling
5. Compare with simpler multi-page documents to isolate the issue

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
