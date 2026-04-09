using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class HeaderFooterLayoutEngineTests
{
	[Fact]
	public void Layout_NullContent_ThrowsArgumentNullException()
	{
		var act = () => HeaderFooterLayoutEngine.Layout(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("content");
	}

	[Fact]
	public void Layout_EmptyContent_ReturnsEmptyWithZeroHeight()
	{
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", []);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(0f);
	}

	[Fact]
	public void Layout_SingleParagraph_UsesDefaultLineHeight()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().ContainSingle();
		blocks[0].Block.Should().BeSameAs(para);
		// Default natural line height = 240 twips, spacing = None (0/0/0/Auto).
		// ComputeParagraphHeight(1, 240) = 0 + 1 * 240 + 0 = 240.
		totalHeight.Should().Be(240f);
		blocks[0].HeightTwips.Should().Be(240f);
	}

	[Fact]
	public void Layout_MultipleParagraphs_SumsTotalHeight()
	{
		var para1 = new ParagraphBlock { SourceElement = new Paragraph() };
		var para2 = new ParagraphBlock { SourceElement = new Paragraph() };
		var para3 = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para1, para2, para3]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().HaveCount(3);
		totalHeight.Should().Be(720f); // 3 × 240
	}

	[Fact]
	public void Layout_WithCustomLineHeight_UsesProvidedValue()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content, naturalLineHeight: 300f);

		totalHeight.Should().Be(300f);
		blocks[0].HeightTwips.Should().Be(300f);
	}

	[Fact]
	public void Layout_WithZeroLineHeight_UsesDefault()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para]);

		var (_, totalHeight) = HeaderFooterLayoutEngine.Layout(content, naturalLineHeight: 0f);

		totalHeight.Should().Be(HeaderFooterLayoutEngine.DefaultNaturalLineHeightTwips);
	}

	[Fact]
	public void Layout_WithNegativeLineHeight_UsesDefault()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para]);

		var (_, totalHeight) = HeaderFooterLayoutEngine.Layout(content, naturalLineHeight: -100f);

		totalHeight.Should().Be(HeaderFooterLayoutEngine.DefaultNaturalLineHeightTwips);
	}

	[Fact]
	public void Layout_NonParagraphBlock_UsesDefaultLineHeight()
	{
		var table = new TablePlaceholderBlock { TableElement = new Table() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [table]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().ContainSingle();
		totalHeight.Should().Be(HeaderFooterLayoutEngine.DefaultNaturalLineHeightTwips);
	}

	[Fact]
	public void Layout_MixedBlockTypes_ComputesCorrectTotal()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var table = new TablePlaceholderBlock { TableElement = new Table() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para, table]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().HaveCount(2);
		totalHeight.Should().Be(480f); // 240 + 240
	}

	// --- Footer-specific tests (step 3.3.3) ---

	[Fact]
	public void Layout_FooterSingleParagraph_ProducesSameResultAsHeader()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var headerContent = new HeaderFooterContent(HeaderFooterKind.Default, "rId1", [para]);
		var footerContent = new HeaderFooterContent(HeaderFooterKind.Default, "rId2", [para]);

		var (headerBlocks, headerHeight) = HeaderFooterLayoutEngine.Layout(headerContent);
		var (footerBlocks, footerHeight) = HeaderFooterLayoutEngine.Layout(footerContent);

		footerBlocks.Should().HaveCount(headerBlocks.Count);
		footerHeight.Should().Be(headerHeight);
	}

	[Fact]
	public void Layout_FooterMultipleParagraphs_SumsTotalHeight()
	{
		var para1 = new ParagraphBlock { SourceElement = new Paragraph() };
		var para2 = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rFooter1", [para1, para2]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().HaveCount(2);
		totalHeight.Should().Be(480f); // 2 × 240
	}

	[Fact]
	public void Layout_FooterWithFirstKind_Works()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.First, "rFooter2", [para]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().ContainSingle();
		totalHeight.Should().Be(240f);
	}

	[Fact]
	public void Layout_FooterWithEvenKind_Works()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var content = new HeaderFooterContent(HeaderFooterKind.Even, "rFooter3", [para]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().ContainSingle();
		totalHeight.Should().Be(240f);
	}

	[Fact]
	public void Layout_FooterWithMixedContent_ComputesCorrectHeight()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var table = new TablePlaceholderBlock { TableElement = new Table() };
		var content = new HeaderFooterContent(HeaderFooterKind.Default, "rFooter4", [para, table]);

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().HaveCount(2);
		totalHeight.Should().Be(480f);
	}
}
