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
	/// Documents where page count mismatches are temporarily accepted while an issue is open.
	/// Keep empty when all page-count regressions are fixed.
	/// </summary>
	private static readonly HashSet<string> KnownPageCountMismatchDocuments =
	[
	];

	/// <summary>
	/// Documents that still do not have Word-generated reference PNGs.
	/// Keep this empty unless a tracked issue requires temporary exclusion.
	/// </summary>
	private static readonly HashSet<string> KnownMissingReferenceDocuments =
	[
		"with-toc",
		"with-tof",
		"with-cross-refs",
	];

	/// <summary>
	/// Field-update corpus docs are validated by <see cref="FieldUpdateVisualRegressionTests"/>
	/// and are excluded here because this suite renders without field update enabled.
	/// </summary>
	private static readonly HashSet<string> FieldUpdateReferenceDocuments =
	[
		"field-update-toc",
		"field-update-tof",
		"field-update-page-of",
		"field-update-cross-refs",
	];

	private readonly ITestOutputHelper _output;

	public VisualRegressionComparisonTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public async Task CorpusDocuments_RenderedPages_MatchReferencePngsWithinThresholds()
	{
		var assetsDir = VisualRegressionSupport.GetAssetsDirectory();
		var docxDir = Path.Combine(assetsDir, "docx");
		var referenceDir = Path.Combine(assetsDir, "reference");

		Directory.Exists(docxDir).Should().BeTrue($"DOCX corpus directory not found: {docxDir}");
		Directory.Exists(referenceDir).Should().BeTrue($"Reference image directory not found: {referenceDir}");

		var thresholds = VisualRegressionThresholds.LoadFromFile(Path.Combine(assetsDir, "thresholds.json"));
		var run = new CorpusRun();

		var corpusFiles = Directory.GetFiles(docxDir, "*.docx", SearchOption.TopDirectoryOnly)
			.Concat(Directory.GetFiles(docxDir, "*.dotx", SearchOption.TopDirectoryOnly))
			.OrderBy(p => p)
			.ToArray();

		foreach (var docxPath in corpusFiles)
		{
			await CompareCorpusDocumentAsync(docxPath, referenceDir, thresholds, run);
		}

		foreach (var line in run.Summaries)
		{
			_output.WriteLine(line);
		}

		foreach (var line in run.KnownGaps)
		{
			_output.WriteLine("KNOWN GAP: " + line);
		}

		run.Failures.Should().BeEmpty("Visual regression mismatches:\n" + string.Join(Environment.NewLine, run.Failures));
	}

	private static async Task CompareCorpusDocumentAsync(string docxPath, string referenceDir, VisualRegressionThresholds thresholds, CorpusRun run)
	{
		var stem = Path.GetFileNameWithoutExtension(docxPath);
		if (FieldUpdateReferenceDocuments.Contains(stem))
		{
			run.KnownGaps.Add($"{stem}: validated in FieldUpdateVisualRegressionTests (requires FieldUpdateOptions).");
			return;
		}

		var maxDeviation = thresholds.GetMaxDeviation(stem);

		using var stream = File.OpenRead(docxPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource(TimeSpan.FromSeconds(45));
		var result = await new DocxRenderer(new RenderOptions()).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		var expectedPageCount = VisualRegressionSupport.CountReferencePages(referenceDir, stem);

		if (!ArePageCountsAcceptable(stem, result.Pages.Count, expectedPageCount, run))
		{
			return;
		}

		var pageSsims = ComparePages(stem, result, referenceDir, maxDeviation, run.Failures);
		if (pageSsims.Count > 0)
		{
			run.Summaries.Add($"{stem}: pages={pageSsims.Count}, minSSIM={pageSsims.Min():F4}, avgSSIM={pageSsims.Average():F4}, maxDeviation={maxDeviation:F4}");
		}
	}

	private static bool ArePageCountsAcceptable(string stem, int renderedCount, int expectedPageCount, CorpusRun run)
	{
		if (expectedPageCount == 0)
		{
			if (KnownMissingReferenceDocuments.Contains(stem))
			{
				run.KnownGaps.Add($"{stem}: no reference PNG (Word COM cannot open this document - see KNOWN_ISSUES.md)");
			}
			else
			{
				run.Failures.Add($"{stem}: no reference PNG files found.");
			}

			return false;
		}

		if (renderedCount == expectedPageCount)
		{
			return true;
		}

		if (KnownPageCountMismatchDocuments.Contains(stem))
		{
			run.KnownGaps.Add($"{stem}: page count mismatch (see KNOWN_ISSUES.md) - Rendered={renderedCount}, Reference={expectedPageCount}");
		}
		else
		{
			run.Failures.Add($"{stem}: page count mismatch. Rendered={renderedCount}, Reference={expectedPageCount}");
		}

		return false;
	}

	private static List<float> ComparePages(string stem, RenderResult result, string referenceDir, float maxDeviation, List<string> failures)
	{
		var minAllowedSsim = 1f - maxDeviation;
		var pageSsims = new List<float>();
		for (var pageIndex = 0; pageIndex < result.Pages.Count; pageIndex++)
		{
			var pageNumber = pageIndex + 1;
			var referencePath = VisualRegressionSupport.ReferencePagePath(referenceDir, stem, pageNumber);
			if (!File.Exists(referencePath))
			{
				failures.Add($"{stem} page {pageNumber}: missing reference PNG: {referencePath}");
				continue;
			}

			var ssim = VisualRegressionSupport.ComputeSsim(referencePath, result.Pages[pageIndex].ToSvg());
			pageSsims.Add(ssim);

			if (ssim < minAllowedSsim)
			{
				failures.Add($"{stem} page {pageNumber}: SSIM {ssim:F4} below minimum {minAllowedSsim:F4} (max deviation {maxDeviation:F4}).");
			}
		}

		return pageSsims;
	}

	private sealed class CorpusRun
	{
		public List<string> Failures { get; } = [];

		public List<string> Summaries { get; } = [];

		public List<string> KnownGaps { get; } = [];
	}
}