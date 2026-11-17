namespace PanoramicData.Render.Test;

using AwesomeAssertions;
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
	public async Task FieldUpdateDocument_RenderedWithFieldUpdate_MatchesWordReference(string stem)
	{
		var assetsDir = VisualRegressionSupport.GetAssetsDirectory();
		var docxPath = Path.Combine(assetsDir, "docx", stem + ".docx");
		var referenceDir = Path.Combine(assetsDir, "reference");

		File.Exists(docxPath).Should().BeTrue($"DOCX not found: {docxPath}");

		var thresholds = VisualRegressionThresholds.LoadFromFile(Path.Combine(assetsDir, "thresholds.json"));
		var minAllowedSsim = 1f - thresholds.GetMaxDeviation(stem);

		// Render with field update enabled: this is the key difference from the standard visual regression test
		using var stream = File.OpenRead(docxPath);
		var options = new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		};
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource(TimeSpan.FromSeconds(45));
		var result = await new DocxRenderer(options).RenderAsync(stream, cts.Token).ConfigureAwait(true);

		_output.WriteLine($"{stem}: rendered {result.Pages.Count} pages");

		var expectedPageCount = VisualRegressionSupport.CountReferencePages(referenceDir, stem);
		expectedPageCount.Should().BeGreaterThan(0, $"no reference PNGs found for {stem}");

		_output.WriteLine($"{stem}: {expectedPageCount} reference pages");

		var failures = new List<string>();
		var pageSsims = ComparePages(stem, result, referenceDir, minAllowedSsim, expectedPageCount, failures);

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

	private List<float> ComparePages(string stem, RenderResult result, string referenceDir, float minAllowedSsim, int expectedPageCount, List<string> failures)
	{
		var pageSsims = new List<float>();
		for (var pageIndex = 0; pageIndex < result.Pages.Count && pageIndex < expectedPageCount; pageIndex++)
		{
			var pageNumber = pageIndex + 1;
			var referencePath = VisualRegressionSupport.ReferencePagePath(referenceDir, stem, pageNumber);
			if (!File.Exists(referencePath))
			{
				failures.Add($"page {pageNumber}: missing reference PNG");
				continue;
			}

			var svg = result.Pages[pageIndex].ToSvg();
			_output.WriteLine($"  page {pageNumber}: SVG length={svg.Length}");

			var ssim = VisualRegressionSupport.ComputeSsim(referencePath, svg);
			pageSsims.Add(ssim);

			_output.WriteLine($"  page {pageNumber}: SSIM={ssim:F4} (min={minAllowedSsim:F4})");

			if (ssim < minAllowedSsim)
			{
				failures.Add($"page {pageNumber}: SSIM {ssim:F4} below minimum {minAllowedSsim:F4}");
			}
		}

		return pageSsims;
	}
}