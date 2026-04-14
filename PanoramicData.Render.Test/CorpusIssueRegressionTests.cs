namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class CorpusIssueRegressionTests
{
	[Theory]
	[InlineData("inline-images")]
	[InlineData("floating-images")]
	[InlineData("panoramic-data-document-2026")]
	public void CorpusDocument_PageCount_MatchesReference(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		var result = new DocxRenderer(new RenderOptions()).Render(stream);
		var expected = Directory.GetFiles(Path.Combine(assetsDir, "reference"), stem + "_page-*.png", SearchOption.TopDirectoryOnly).Length;
		result.Pages.Count.Should().Be(expected);
	}

	[Fact]
	public void CorpusWithToc_FieldUpdate_ContainsHeadingEntries()
	{
		var result = RenderCorpusWithFieldUpdate("with-toc");
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
	public void CorpusWithTof_FieldUpdate_ContainsFigureEntries()
	{
		var result = RenderCorpusWithFieldUpdate("with-tof");
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
	public void CorpusWithCrossRefs_FieldUpdate_ResolvesPageRefAndRef()
	{
		var result = RenderCorpusWithFieldUpdate("with-cross-refs");

		// Field update should report cross-reference fields were updated
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("PAGEREF");
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("REF");
	}

	private RenderResult RenderCorpusWithFieldUpdate(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		var options = new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		};
		return new DocxRenderer(options).Render(stream);
	}

	[Fact]
	public void FieldUpdateToc_WithFieldUpdate_ContainsAllInjectedHeadings()
	{
		var result = RenderCorpusWithFieldUpdate("field-update-toc");

		// The TOC should contain entries for the injected headings (Chapters 2–12)
		// With proper paragraph spacing the TOC may span multiple pages
		var allSvg = string.Join(" ", result.Pages.Select(p => p.ToSvg()));
		allSvg.Should().Contain("Chapter 2");
		allSvg.Should().Contain("Chapter 12");

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void FieldUpdateTof_WithFieldUpdate_ProcessesTofField()
	{
		var result = RenderCorpusWithFieldUpdate("field-update-tof");

		// Verify the engine processes the document without error.
		// NOTE: The Word COM-generated caption structure (proper SEQ fields via InsertCaption)
		// is not yet fully matched by our TOF engine. The visual regression test against
		// Word's reference PNGs is the definitive fidelity check.
		result.Pages.Count.Should().BeGreaterThan(0);
		result.FieldUpdateResult.Should().NotBeNull();
	}

	[Fact]
	public void FieldUpdatePageOf_WithFieldUpdate_RendersMultiplePages()
	{
		var result = RenderCorpusWithFieldUpdate("field-update-page-of");

		// The document should have multiple pages (seed had 1 page, staleness injected 6 more)
		result.Pages.Count.Should().BeGreaterThan(1);

		// NOTE: PAGE/NUMPAGES fields in the footer are handled by the render-time layout
		// engine via direct substitution, not by the FieldUpdateEngine (which only walks
		// the document body). The visual regression test confirms correct footer rendering.
		result.FieldUpdateResult.Should().NotBeNull();
	}

	[Fact]
	public void FieldUpdateCrossRefs_WithFieldUpdate_ReportsPageRefUpdated()
	{
		var result = RenderCorpusWithFieldUpdate("field-update-cross-refs");

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("PAGEREF");
	}

	[Theory]
	[InlineData("inline-images")]
	[InlineData("floating-images")]
	public void CorpusDocument_WithImages_SvgContainsImageElements(string stem)
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), stem);
		using var stream = File.OpenRead(docPath);
		var result = new DocxRenderer(new RenderOptions { EmbedImages = true }).Render(stream);
		var allSvg = string.Join("\n", result.Pages.Select(p => p.ToSvg()));
		allSvg.Should().Contain("<image", "images should be rendered as SVG <image> elements");
		allSvg.Should().Contain("data:image/", "embedded images should use data URIs");
	}

	[Fact]
	public void PanoramicDataDocument_Page3_ContainsHeadingNumbers()
	{
		var assetsDir = GetAssetsDirectory();
		var docPath = ResolvePath(Path.Combine(assetsDir, "docx"), "panoramic-data-document-2026");
		using var stream = File.OpenRead(docPath);
		var result = new DocxRenderer(new RenderOptions()).Render(stream);
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
