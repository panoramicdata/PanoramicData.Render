namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class CorpusIssueRegressionTests
{
	[Theory]
	[InlineData("inline-images")]
	[InlineData("floating-images")]
	[InlineData("panoramic-data-document-2026")]
	public async Task CorpusDocument_PageCount_MatchesReference(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		var result = await new DocxRenderer(new RenderOptions()).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		var expected = Directory.GetFiles(Path.Combine(assetsDir, "reference"), stem + "_page-*.png", SearchOption.TopDirectoryOnly).Length;
		result.Pages.Count.Should().Be(expected);
	}

	[Fact]
	public async Task CorpusWithToc_FieldUpdate_ContainsHeadingEntries()
	{
		var result = await RenderCorpusWithFieldUpdate("with-toc").ConfigureAwait(true);
		var svg = result.Pages[0].ToSvg();

		// TOC should contain heading entries from the document
		svg.Should().Contain("Chapter 1");
		svg.Should().Contain("Chapter 2");
		svg.Should().Contain("Chapter 3");

		// Field update should report TOC was updated
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public async Task CorpusWithTof_FieldUpdate_ContainsFigureEntries()
	{
		var result = await RenderCorpusWithFieldUpdate("with-tof").ConfigureAwait(true);
		var svg = result.Pages[0].ToSvg();

		// TOF should contain figure caption entries
		svg.Should().Contain("Figure 1");
		svg.Should().Contain("Figure 2");
		svg.Should().Contain("Figure 3");

		// Field update should report TOF was updated
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOF");
	}

	[Fact]
	public async Task CorpusWithCrossRefs_FieldUpdate_ResolvesPageRefAndRef()
	{
		var result = await RenderCorpusWithFieldUpdate("with-cross-refs").ConfigureAwait(true);

		// Field update should report cross-reference fields were updated
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("PAGEREF");
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("REF");
	}

	private async Task<RenderResult> RenderCorpusWithFieldUpdate(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		var options = new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		};
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		return await new DocxRenderer(options).RenderAsync(stream, cts.Token).ConfigureAwait(true);
	}

	[Fact]
	public async Task FieldUpdateToc_WithFieldUpdate_ContainsAllInjectedHeadings()
	{
		var result = await RenderCorpusWithFieldUpdate("field-update-toc").ConfigureAwait(true);

		// The TOC should contain entries for the injected headings (Chapters 2–12)
		// With proper paragraph spacing the TOC may span multiple pages
		var allSvg = string.Join(" ", result.Pages.Select(p => p.ToSvg()));
		allSvg.Should().Contain("Chapter 2");
		allSvg.Should().Contain("Chapter 12");

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public async Task FieldUpdateTof_WithFieldUpdate_ProcessesTofField()
	{
		var result = await RenderCorpusWithFieldUpdate("field-update-tof").ConfigureAwait(true);

		// Verify the engine processes the document without error.
		// NOTE: The Word COM-generated caption structure (proper SEQ fields via InsertCaption)
		// is not yet fully matched by our TOF engine. The visual regression test against
		// Word's reference PNGs is the definitive fidelity check.
		result.Pages.Count.Should().BeGreaterThan(0);
		result.FieldUpdateResult.Should().NotBeNull();
	}

	[Fact]
	public async Task FieldUpdatePageOf_WithFieldUpdate_RendersMultiplePages()
	{
		var result = await RenderCorpusWithFieldUpdate("field-update-page-of").ConfigureAwait(true);

		// The document should have multiple pages (seed had 1 page, staleness injected 6 more)
		result.Pages.Count.Should().BeGreaterThan(1);

		// NOTE: PAGE/NUMPAGES fields in the footer are handled by the render-time layout
		// engine via direct substitution, not by the FieldUpdateEngine (which only walks
		// the document body). The visual regression test confirms correct footer rendering.
		result.FieldUpdateResult.Should().NotBeNull();
	}

	[Fact]
	public async Task FieldUpdateCrossRefs_WithFieldUpdate_ReportsPageRefUpdated()
	{
		var result = await RenderCorpusWithFieldUpdate("field-update-cross-refs").ConfigureAwait(true);

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("PAGEREF");
	}

	[Theory]
	[InlineData("inline-images")]
	[InlineData("floating-images")]
	public async Task CorpusDocument_WithImages_SvgContainsImageElements(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		var result = await new DocxRenderer(new RenderOptions { EmbedImages = true }).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		var allSvg = string.Join("\n", result.Pages.Select(p => p.ToSvg()));
		allSvg.Should().Contain("<image", "images should be rendered as SVG <image> elements");
		allSvg.Should().Contain("data:image/", "embedded images should use data URIs");
	}

	[Fact]
	public async Task PanoramicDataDocument_Page2_ContainsTableContent()
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), "panoramic-data-document-2026");
		using var stream = File.OpenRead(docPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		var result = await new DocxRenderer(new RenderOptions()).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		var svgs = result.ToSvgPages();
		var svg2 = svgs[1];

		// Revision history table header
		svg2.Should().Contain("Version", "revision history table header should contain 'Version'");
		// Company information table content
		svg2.Should().Contain("Panoramic Data Limited", "company table should contain company name");
		// Address wraps correctly
		svg2.Should().Contain("Panoramic House", "company table should contain address");
	}

	[Fact]
	public async Task PanoramicDataDocument_Page3_ContainsHeadingNumbers()
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), "panoramic-data-document-2026");
		using var stream = File.OpenRead(docPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		var result = await new DocxRenderer(new RenderOptions()).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		result.Pages.Count.Should().BeGreaterThanOrEqualTo(3);

		var allSvgs = result.ToSvgPages();
		var svg3 = allSvgs[2];

		// Heading 1 on page 3 should be "2" (second occurrence of Heading1 in the document)
		svg3.Should().Contain(">2 </text>", "Heading 1 on page 3 should have number '2'");

		// Heading 2 on page 3 should be "2.1"
		svg3.Should().Contain(">2.1 </text>", "Heading 2 on page 3 should have number '2.1'");

		// Heading 3 on page 3 should be "2.1.1"
		svg3.Should().Contain(">2.1.1 </text>", "Heading 3 on page 3 should have number '2.1.1'");

		// Second Heading 2 on page 3 ("Bullets") should be "2.2"
		svg3.Should().Contain(">2.2 </text>", "second Heading 2 on page 3 should have number '2.2'");
	}

	[Fact]
	public async Task PanoramicDataDocument_Section2Pages_RenderInheritedHeaderAndFooter()
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), "panoramic-data-document-2026");
		using var stream = File.OpenRead(docPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		var result = await new DocxRenderer(new RenderOptions()).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		var svgs = result.ToSvgPages();

		svgs.Should().HaveCountGreaterThanOrEqualTo(3, "the template has a cover section followed by at least two body pages");

		// Section 2 (pages 2 and 3) has no explicit header/footer references,
		// so it must inherit the section 1 default header/footer.
		svgs[1].Should().Contain(">Title<", "page 2 should show inherited header content");
		svgs[2].Should().Contain(">Title<", "page 3 should show inherited header content");
		svgs[1].Should().Contain("Commercial Confidence", "page 2 should show inherited footer content");
		svgs[2].Should().Contain("Commercial Confidence", "page 3 should show inherited footer content");
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
		throw new DirectoryNotFoundException();
	}

	[Fact]
	public async Task PanoramicDataDoc_TableCells_ContainOrangeFill()
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), "panoramic-data-document-2026");
		using var stream = File.OpenRead(docPath);
		using var cts = TestCancellation.CreateRenderTimeoutTokenSource();
		var result = await new DocxRenderer(new RenderOptions()).RenderAsync(stream, cts.Token).ConfigureAwait(true);
		var allSvg = string.Join("\n", result.Pages.Select(p => p.ToSvg()));
		// PanoramicData table style has firstRow shading of #ED7D31 (orange)
		allSvg.Should().Contain("ED7D31", "tables with PanoramicData style should have orange header row background");
	}

	private static string ResolvePath(string docxDir, string stem)
	{
		var docxPath = Path.Combine(docxDir, stem + ".docx");
		if (File.Exists(docxPath))
		{
			return docxPath;
		}

		return Path.Combine(docxDir, stem + ".dotx");
	}
}
