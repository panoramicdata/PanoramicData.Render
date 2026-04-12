# Test Assets

This folder contains the visual regression corpus used by tests.

## Structure

- `docx/` contains generated source documents.
- `reference/` contains Word-ground-truth PNG pages at 150 DPI.

## Regeneration

From the repository root:

`dotnet run --project PanoramicData.Render.ReferenceGenerator -- all PanoramicData.Render.Test/test-assets/docx PanoramicData.Render.Test/test-assets/reference`

You can also run the stages separately:

- `dotnet run --project PanoramicData.Render.ReferenceGenerator -- generate-corpus PanoramicData.Render.Test/test-assets/docx`
- `dotnet run --project PanoramicData.Render.ReferenceGenerator -- render PanoramicData.Render.Test/test-assets/docx PanoramicData.Render.Test/test-assets/reference`

Output files use this naming convention:

`{docx-stem}_page-{N}.png` where `N` is a 1-indexed page number.
