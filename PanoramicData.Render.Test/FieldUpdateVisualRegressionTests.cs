namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

/// <summary>
/// Visual regression tests for the field-update engine.
/// These render documents that have deliberately stale field values,
/// with field update enabled, and compare against Word-generated reference PNGs
/// that were produced by Word's own Fields.Update() + PDF export.
/// </summary>
public sealed class FieldUpdateVisualRegressionTests
{
	/// <summary>
	/// Field-update corpus document stems (must match the FieldUpdateCorpusGenerator output).
	/// </summary>
	private static readonly string[] FieldUpdateDocuments =
	[
		"field-update-toc",
		"field-update-tof",
		"field-update-page-of",
		"field-update-cross-refs",
	];

	private readonly ITestOutputHelper _output;

	public FieldUpdateVisualRegressionTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Theory]
	[InlineData("field-update-toc")]
	[InlineData("field-update-tof")]
	[InlineData("field-update-page-of")]
	[InlineData("field-update-cross-refs")]
	public void FieldUpdateDocument_RenderedWithFieldUpdate_MatchesWordReference(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docxPath = Path.Combine(assetsDir, "docx", stem + ".docx");
		var referenceDir = Path.Combine(assetsDir, "reference");
		var thresholdsPath = Path.Combine(assetsDir, "thresholds.json");

		File.Exists(docxPath).Should().BeTrue($"DOCX not found: {docxPath}");

		var thresholds = VisualRegressionThresholds.LoadFromFile(thresholdsPath);
		var maxDeviation = thresholds.GetMaxDeviation(stem);
		var minAllowedSsim = 1f - maxDeviation;

		// Render with field update enabled — this is the key difference from the standard visual regression test
		using var stream = File.OpenRead(docxPath);
		var options = new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		};
		var result = new DocxRenderer(options).Render(stream);

		_output.WriteLine($"{stem}: rendered {result.Pages.Count} pages");

		var expectedPageCount = Directory.GetFiles(referenceDir, $"{stem}_page-*.png", SearchOption.TopDirectoryOnly).Length;
		expectedPageCount.Should().BeGreaterThan(0, $"no reference PNGs found for {stem}");

		_output.WriteLine($"{stem}: {expectedPageCount} reference pages");

		var failures = new List<string>();
		var pageSsims = new List<float>();

		for (var pageIndex = 0; pageIndex < result.Pages.Count && pageIndex < expectedPageCount; pageIndex++)
		{
			var pageNumber = pageIndex + 1;
			var referencePath = Path.Combine(referenceDir, $"{stem}_page-{pageNumber}.png");
			if (!File.Exists(referencePath))
			{
				failures.Add($"page {pageNumber}: missing reference PNG");
				continue;
			}

			var svg = result.Pages[pageIndex].ToSvg();
			_output.WriteLine($"  page {pageNumber}: SVG length={svg.Length}");

			var actualPng = SvgRasterizer.RasterizeToPng(svg, 150f);
			var expectedPng = File.ReadAllBytes(referencePath);

			using var expectedBitmap = SKBitmap.Decode(expectedPng);
			using var actualBitmap = SKBitmap.Decode(actualPng);
			using var normalizedActual = NormalizeToSize(actualBitmap, expectedBitmap.Width, expectedBitmap.Height);

			var ssim = PerceptualImageDiff.ComputeSsim(expectedBitmap, normalizedActual);
			pageSsims.Add(ssim);

			_output.WriteLine($"  page {pageNumber}: SSIM={ssim:F4} (min={minAllowedSsim:F4})");

			if (ssim < minAllowedSsim)
			{
				failures.Add($"page {pageNumber}: SSIM {ssim:F4} below minimum {minAllowedSsim:F4}");
			}
		}

		if (result.Pages.Count != expectedPageCount)
		{
			failures.Add($"page count mismatch: rendered={result.Pages.Count}, reference={expectedPageCount}");
		}

		if (pageSsims.Count > 0)
		{
			_output.WriteLine($"{stem}: pages={pageSsims.Count}, minSSIM={pageSsims.Min():F4}, avgSSIM={pageSsims.Average():F4}");
		}

		failures.Should().BeEmpty($"Visual regression failures for {stem}:\n" + string.Join(Environment.NewLine, failures));
	}

	private static SKBitmap NormalizeToSize(SKBitmap bitmap, int width, int height)
	{
		if (bitmap.Width == width && bitmap.Height == height)
		{
			return bitmap.Copy();
		}

		var resized = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
		using var canvas = new SKCanvas(resized);
		canvas.Clear(SKColors.White);
		canvas.DrawBitmap(bitmap, new SKRect(0, 0, width, height));
		return resized;
	}

	private static string GetAssetsDirectory()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null)
		{
			if (File.Exists(Path.Combine(current.FullName, "PanoramicData.Render.slnx")))
			{
				return Path.Combine(current.FullName, "PanoramicData.Render.Test", "test-assets");
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate repository root from test base directory.");
	}
}
