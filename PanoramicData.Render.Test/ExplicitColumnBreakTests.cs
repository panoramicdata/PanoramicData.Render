namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class ExplicitColumnBreakTests
{
	private static readonly SectionInfo SingleColumnSection = new();
	private static readonly SectionInfo TwoColumnSection = new() { ColumnCount = 2 };

	[Fact]
	public void Paginate_ForceColumnBreakBefore_InTwoColumnSection_AdvancesToNextColumn()
	{
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeBlock(1000f, forceColumnBreak: true),
			MakeBlock(1000f),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(3);
		result[0].BlockPlacements.Should().HaveCount(3);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);
		result[0].BlockPlacements[2].ColumnIndex.Should().Be(1);
	}

	[Fact]
	public void Paginate_ForceColumnBreakBefore_OnLastColumn_StartsNewPage()
	{
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f),
			MakeBlock(1000f, forceColumnBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[1].Blocks.Should().ContainSingle();
		result[1].BlockPlacements.Should().ContainSingle();
		result[1].BlockPlacements[0].ColumnIndex.Should().Be(0);
	}

	[Fact]
	public void Paginate_ForceColumnBreakBefore_InSingleColumnSection_StartsNewPage()
	{
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeBlock(1000f, forceColumnBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, SingleColumnSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void LayoutBlock_ForceColumnBreakBefore_DefaultIsFalse()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f);

		block.ForceColumnBreakBefore.Should().BeFalse();
	}

	[Fact]
	public void LayoutBlock_ForceColumnBreakBefore_CanBeSetTrue()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f, ForceColumnBreakBefore: true);

		block.ForceColumnBreakBefore.Should().BeTrue();
	}

	private static LayoutBlock MakeBlock(float heightTwips, bool forceColumnBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips, ForceColumnBreakBefore: forceColumnBreak);
	}
}