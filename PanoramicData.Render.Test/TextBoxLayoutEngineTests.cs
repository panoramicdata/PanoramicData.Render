namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class TextBoxLayoutEngineTests
{
	[Fact]
	public void Layout_NullTextFrame_ThrowsArgumentNullException()
	{
		var act = () => TextBoxLayoutEngine.Layout(null!, 2400f);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("textFrame");
	}

	[Fact]
	public void Layout_NonPositiveWidth_ThrowsArgumentOutOfRangeException()
	{
		var textFrame = new ShapeTextFrameInfo { HasTextFrame = true, Text = "Hello" };

		var act = () => TextBoxLayoutEngine.Layout(textFrame, 0f);

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("availableWidthTwips");
	}

	[Fact]
	public void Layout_TextFrameWithoutContent_ReturnsEmpty()
	{
		var textFrame = ShapeTextFrameInfo.None;

		var (blocks, totalHeight) = TextBoxLayoutEngine.Layout(textFrame, 2400f);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(0f);
	}

	[Fact]
	public void Layout_ParagraphBlocks_UsesParagraphLineBreaker()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Blocks =
			[
				CreateParagraphBlock("The quick brown fox jumps over the lazy dog again and again"),
				CreateParagraphBlock("Second paragraph")
			]
		};

		var (blocks, totalHeight) = TextBoxLayoutEngine.Layout(textFrame, 1200f, fontFamily: "Arial");

		blocks.Should().HaveCount(2);
		blocks[0].Block.Should().BeOfType<ParagraphBlock>();
		blocks[0].LineHeights.Should().NotBeNull();
		var firstLineHeights = blocks[0].LineHeights!;
		firstLineHeights.Count.Should().BeGreaterThan(1);
		blocks[0].HeightTwips.Should().Be(firstLineHeights.Sum());
		blocks[1].Block.Should().BeOfType<ParagraphBlock>();
		blocks[1].LineHeights.Should().NotBeNull();
		blocks[1].LineHeights!.Count.Should().BeGreaterThanOrEqualTo(1);
		totalHeight.Should().Be(blocks.Sum(block => block.HeightTwips));
	}

	[Fact]
	public void Layout_TableBlock_UsesTableLayoutHeight()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }),
			new TableRow(
				new TableCell(
					new Paragraph(new Run(new Text("Cell text"))))));
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Blocks = [new TablePlaceholderBlock { TableElement = table }]
		};
		var expectedHeight = TableLayoutEngine.Layout(TableParser.Parse(table), 3600f).TotalHeightTwips;

		var (blocks, totalHeight) = TextBoxLayoutEngine.Layout(textFrame, 3600f);

		blocks.Should().ContainSingle();
		blocks[0].Block.Should().BeOfType<TablePlaceholderBlock>();
		blocks[0].HeightTwips.Should().Be(expectedHeight);
		totalHeight.Should().Be(expectedHeight);
	}

	[Fact]
	public void Layout_TextOnlyFrame_SynthesizesParagraphBlocks()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "First line\nSecond line"
		};

		var (blocks, totalHeight) = TextBoxLayoutEngine.Layout(textFrame, 2400f);

		blocks.Should().HaveCount(2);
		blocks.Should().OnlyContain(block => block.Block is ParagraphBlock);
		totalHeight.Should().Be(2 * TextBoxLayoutEngine.DefaultLineHeightTwips);
	}

	[Fact]
	public void Layout_WithHorizontalInsets_UsesReducedContentWidth()
	{
		var textFrame = new ShapeTextFrameInfo
		{
			HasTextFrame = true,
			Text = "One two three four five six seven eight nine ten",
			LeftInsetEmu = 19050,
			RightInsetEmu = 19050
		};
		var noInsetFrame = textFrame with { LeftInsetEmu = 0, RightInsetEmu = 0 };

		var (noInsetBlocks, _) = TextBoxLayoutEngine.Layout(noInsetFrame, 2000f, fontFamily: "Arial");
		var (insetBlocks, _) = TextBoxLayoutEngine.Layout(textFrame, 2000f, fontFamily: "Arial");

		var noInsetLineCount = noInsetBlocks[0].LineHeights!.Count;
		var insetLineCount = insetBlocks[0].LineHeights!.Count;
		TextBoxLayoutEngine.GetContentWidthTwips(textFrame, 2000f).Should().BeApproximately(1940f, 0.001f);
		insetLineCount.Should().BeGreaterThanOrEqualTo(noInsetLineCount);
	}

	private static ParagraphBlock CreateParagraphBlock(string text)
	{
		return DocumentBlockParser.CreateParagraphBlock(
			new Paragraph(new Run(new Text(text))));
	}
}