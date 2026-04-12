# Visual Regression Baseline Generation Workflow

## Reference Word Version

Baselines are generated using **Microsoft Word for Microsoft 365** (Version 2506, Build 18925.20000 or later).
Pin to a specific build to ensure reproducibility. Record the exact Word build when generating new baselines.

## Baseline Generation Steps

1. **Open the test DOCX** in the pinned Word version.
2. **Print to PDF** using "Microsoft Print to PDF" (File → Save As → PDF) with default settings.
3. **Rasterize the PDF** to PNG at **150 DPI** using a pinned `Magick.NET` (ImageMagick) or `Ghostscript` build.
4. **Store the reference PNG** in `test-assets/baselines/<document-name>/page-<N>.png`.

## Reference Rasterizer

Use **Magick.NET** (`Magick.NET-Q16-AnyCPU`) with Ghostscript for PDF rasterization:

```csharp
using var images = new MagickImageCollection();
var settings = new MagickReadSettings { Density = new Density(150, 150) };
images.Read("reference.pdf", settings);
for (var i = 0; i < images.Count; i++)
{
    images[i].Write($"page-{i + 1}.png");
}
```

Pin the Magick.NET and Ghostscript versions in `Directory.Packages.props`.

## Comparison Method

PNGs are compared using **perceptual image diff**, not raw pixel comparison.
This avoids false positives from anti-aliasing, sub-pixel rendering, and minor font hinting differences.

Algorithm: Structural Similarity Index (SSIM) via SkiaSharp pixel comparison with a configurable threshold.

## Threshold Configuration

Per-document thresholds are defined in a JSON file (`test-assets/thresholds.json`):

```json
{
  "basic-text-formatting": { "maxSsimDeviation": 0.02 },
  "complex-table": { "maxSsimDeviation": 0.05 },
  "default": { "maxSsimDeviation": 0.03 }
}
```

## Updating Baselines

When a rendering improvement is made that intentionally changes output:

1. Re-generate the reference PNGs using the same pinned Word version.
2. Update the baseline PNGs in `test-assets/baselines/`.
3. Commit with a message explaining why baselines changed.

## CI Integration

Visual regression tests run on every PR. Failed diffs are uploaded as CI artifacts for review.
