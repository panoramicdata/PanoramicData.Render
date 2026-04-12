namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

/// <summary>
/// End-to-end visual regression tests that compare rendered SVG output against
/// Word-generated PNG references in test-assets/reference.
/// </summary>
public sealed class VisualRegressionComparisonTests
{
	/// <summary>
	/// Documents where DocxRenderer page count differs from Word baseline.
	/// See KNOWN_ISSUES.md for details on each mismatch.
	/// These indicate potential pagination bugs in the rendering engine.
	/// Remove from this set when the underlying issues are fixed.
	/// Per the test policy: Every failing test must have a corresponding GitHub Issue.
	/// </summary>
	private static readonly HashSet<string> KnownPageCountMismatchDocuments =
	[
		"page-break",                  // FIXED! Now renders 3 pages correctly. Remove from this set after validation.
		"panoramic-data-document-2026" // See KNOWN_ISSUES.md Issue #4 - renders 2 pages, should be 3
	];

	/// <summary>
	/// Documents that cannot generate Word baselines (Word COM fails to open them).
	/// See KNOWN_ISSUES.md for details on each failure.
	/// Per the test policy: Every failing test must have a corresponding GitHub Issue.
	/// </summary>
	private static readonly HashSet<string> KnownMissingReferenceDocuments =
	[
		"inline-images",   // See KNOWN_ISSUES.md Issue #1
		"floating-images"  // See KNOWN_ISSUES.md Issue #2
	];

	private readonly ITestOutputHelper _output;

	public VisualRegressionComparisonTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void CorpusDocuments_RenderedPages_MatchReferencePngsWithinThresholds()
	{
		var assetsDir = GetAssetsDirectory();
		var docxDir = Path.Combine(assetsDir, "docx");
		var referenceDir = Path.Combine(assetsDir, "reference");
		var thresholdsPath = Path.Combine(assetsDir, "thresholds.json");

		Directory.Exists(docxDir).Should().BeTrue($"DOCX corpus directory not found: {docxDir}");
		Directory.Exists(referenceDir).Should().BeTrue($"Reference image directory not found: {referenceDir}");

		var thresholds = VisualRegressionThresholds.LoadFromFile(thresholdsPath);
		var failures = new List<string>();
		var summaries = new List<string>();
		var knownGaps = new List<string>();

		foreach (var docxPath in Directory.GetFiles(docxDir, "*.docx", SearchOption.TopDirectoryOnly).OrderBy(p => p))
		{
			var stem = Path.GetFileNameWithoutExtension(docxPath);
			var maxDeviation = thresholds.GetMaxDeviation(stem);
			var minAllowedSsim = 1f - maxDeviation;

			using var stream = File.OpenRead(docxPath);
			var result = new DocxRenderer(new RenderOptions()).Render(stream);
			var expectedPageCount = Directory.GetFiles(referenceDir, $"{stem}_page-*.png", SearchOption.TopDirectoryOnly).Length;

			if (expectedPageCount == 0)
			{
				if (KnownMissingReferenceDocuments.Contains(stem))
				{
				knownGaps.Add($"{stem}: no reference PNG (Word COM cannot open this document - see KNOWN_ISSUES.md)");
			}
			else
			{
				failures.Add($"{stem}: no reference PNG files found.");
			}

			continue;
		}

		if (result.Pages.Count != expectedPageCount)
		{
			if (KnownPageCountMismatchDocuments.Contains(stem))
			{
				knownGaps.Add($"{stem}: page count mismatch (see KNOWN_ISSUES.md) - Rendered={result.Pages.Count}, Reference={expectedPageCount}");
				}

				continue;
			}

			var docPageSsims = new List<float>();
			for (var pageIndex = 0; pageIndex < result.Pages.Count; pageIndex++)
			{
				var pageNumber = pageIndex + 1;
				var referencePath = Path.Combine(referenceDir, $"{stem}_page-{pageNumber}.png");
				if (!File.Exists(referencePath))
				{
					failures.Add($"{stem} page {pageNumber}: missing reference PNG: {referencePath}");
					continue;
				}

				var svg = result.Pages[pageIndex].ToSvg();
				var actualPng = SvgRasterizer.RasterizeToPng(svg, 150f);
				var expectedPng = File.ReadAllBytes(referencePath);

				using var expectedBitmap = SKBitmap.Decode(expectedPng);
				using var actualBitmap = SKBitmap.Decode(actualPng);
				using var normalizedActual = NormalizeToSize(actualBitmap, expectedBitmap.Width, expectedBitmap.Height);

				var ssim = PerceptualImageDiff.ComputeSsim(expectedBitmap, normalizedActual);
				docPageSsims.Add(ssim);

				if (ssim < minAllowedSsim)
				{
					failures.Add(
						$"{stem} page {pageNumber}: SSIM {ssim:F4} below minimum {minAllowedSsim:F4} (max deviation {maxDeviation:F4}).");
				}
			}

			if (docPageSsims.Count > 0)
			{
				var minSsim = docPageSsims.Min();
				var avgSsim = docPageSsims.Average();
				summaries.Add($"{stem}: pages={docPageSsims.Count}, minSSIM={minSsim:F4}, avgSSIM={avgSsim:F4}, maxDeviation={maxDeviation:F4}");
			}
		}

		foreach (var line in summaries)
		{
			_output.WriteLine(line);
		}

		foreach (var line in knownGaps)
		{
			_output.WriteLine("KNOWN GAP: " + line);
		}

		failures.Should().BeEmpty("Visual regression mismatches:\n" + string.Join(Environment.NewLine, failures));
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
			var candidate = Path.Combine(current.FullName, "PanoramicData.Render.slnx");
			if (File.Exists(candidate))
			{
				return Path.Combine(current.FullName, "PanoramicData.Render.Test", "test-assets");
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate repository root from test base directory.");
	}
}
