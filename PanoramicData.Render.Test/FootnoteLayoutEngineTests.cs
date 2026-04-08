namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class FootnoteLayoutEngineTests
{
	[Fact]
	public void Layout_NullFootnotes_ThrowsArgumentNullException()
	{
		var act = () => FootnoteLayoutEngine.Layout(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("footnotes");
	}

	[Fact]
	public void Layout_EmptyFootnotes_ReturnsEmptyWithZeroHeight()
	{
		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([]);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(0f);
	}

	[Fact]
	public void Layout_SingleFootnoteOneParagraph_IncludesSeparator()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote]);

		blocks.Should().HaveCount(2);
		blocks[0].Block.Should().BeOfType<FootnoteSeparatorBlock>();
		blocks[0].HeightTwips.Should().Be(FootnoteLayoutEngine.DefaultSeparatorHeightTwips);
		blocks[1].Block.Should().BeOfType<ParagraphBlock>();
		// Separator (240) + 1 paragraph at 200 = 440.
		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultSeparatorHeightTwips + FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void Layout_SingleFootnoteOneParagraph_WithoutSeparator()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote], includeSeparator: false);

		blocks.Should().ContainSingle();
		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void Layout_MultipleFootnotes_SumsAllBlocks()
	{
		var fn1 = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);
		var fn2 = new NoteDefinition(2, null, [
			new ParagraphBlock { SourceElement = new Paragraph() },
			new ParagraphBlock { SourceElement = new Paragraph() },
		]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([fn1, fn2]);

		blocks.Should().HaveCount(4); // separator + 3 paragraphs
		blocks[0].Block.Should().BeOfType<FootnoteSeparatorBlock>();
		// Separator (240) + 3 × 200 = 840.
		totalHeight.Should().Be(840f);
	}

	[Fact]
	public void Layout_WithCustomLineHeight_UsesProvidedValue()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote], naturalLineHeight: 180f);

		blocks.Should().HaveCount(2); // separator + 1 paragraph
		// Separator (240) + 1 × 180 = 420.
		totalHeight.Should().Be(420f);
	}

	[Fact]
	public void Layout_WithZeroLineHeight_UsesDefault()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (_, totalHeight) = FootnoteLayoutEngine.Layout([footnote], naturalLineHeight: 0f, includeSeparator: false);

		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void Layout_WithNegativeLineHeight_UsesDefault()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (_, totalHeight) = FootnoteLayoutEngine.Layout([footnote], naturalLineHeight: -50f, includeSeparator: false);

		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void Layout_NonParagraphBlock_UsesLineHeight()
	{
		var footnote = new NoteDefinition(1, null, [new TablePlaceholderBlock { TableElement = new Table() }]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote], includeSeparator: false);

		blocks.Should().ContainSingle();
		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultFootnoteLineHeightTwips);
	}

	[Fact]
	public void Layout_FootnoteWithEmptyBlocks_SeparatorBlockOnly()
	{
		var footnote = new NoteDefinition(1, null, []);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote]);

		// Separator block is emitted even when footnotes have no content blocks.
		blocks.Should().ContainSingle();
		blocks[0].Block.Should().BeOfType<FootnoteSeparatorBlock>();
		totalHeight.Should().Be(FootnoteLayoutEngine.DefaultSeparatorHeightTwips);
	}

	[Fact]
	public void Layout_MixedBlockTypes_CorrectTotal()
	{
		var footnote = new NoteDefinition(1, null, [
			new ParagraphBlock { SourceElement = new Paragraph() },
			new TablePlaceholderBlock { TableElement = new Table() },
		]);

		var (blocks, totalHeight) = FootnoteLayoutEngine.Layout([footnote], includeSeparator: false);

		blocks.Should().HaveCount(2);
		totalHeight.Should().Be(400f); // 200 + 200
	}

	[Fact]
	public void LayoutPage_FootnoteBlocks_CanBeSet()
	{
		var block = new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph() }, 200f);
		var page = new LayoutPage
		{
			Section = new SectionInfo(),
			PageNumber = 1,
			Blocks = [],
			FootnoteBlocks = [block],
			FootnoteTopTwips = 12000f,
		};

		page.FootnoteBlocks.Should().ContainSingle();
		page.FootnoteTopTwips.Should().Be(12000f);
	}

	[Fact]
	public void LayoutPage_NullFootnoteBlocks_IsDefault()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo(),
			PageNumber = 1,
			Blocks = [],
		};

		page.FootnoteBlocks.Should().BeNull();
		page.FootnoteTopTwips.Should().Be(0f);
	}

	[Fact]
	public void Layout_SeparatorBlock_HasDefaultWidthFraction()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (blocks, _) = FootnoteLayoutEngine.Layout([footnote]);

		var separator = blocks[0].Block.Should().BeOfType<FootnoteSeparatorBlock>().Subject;
		separator.WidthFraction.Should().Be(FootnoteSeparatorBlock.DefaultWidthFraction);
	}

	[Fact]
	public void Layout_NoSeparator_DoesNotContainSeparatorBlock()
	{
		var footnote = new NoteDefinition(1, null, [new ParagraphBlock { SourceElement = new Paragraph() }]);

		var (blocks, _) = FootnoteLayoutEngine.Layout([footnote], includeSeparator: false);

		blocks.Should().AllSatisfy(b => b.Block.Should().NotBeOfType<FootnoteSeparatorBlock>());
	}

	[Fact]
	public void FootnoteSeparatorBlock_DefaultWidthFraction_IsOneThird()
	{
		var separator = new FootnoteSeparatorBlock();

		separator.WidthFraction.Should().BeApproximately(1f / 3f, 0.0001f);
	}

	[Fact]
	public void FootnoteSeparatorBlock_CustomWidthFraction_CanBeSet()
	{
		var separator = new FootnoteSeparatorBlock { WidthFraction = 0.5f };

		separator.WidthFraction.Should().Be(0.5f);
	}

	[Fact]
	public void FootnoteSeparatorBlock_IsDocumentBlock()
	{
		var separator = new FootnoteSeparatorBlock();

		separator.Should().BeAssignableTo<DocumentBlock>();
	}
}
