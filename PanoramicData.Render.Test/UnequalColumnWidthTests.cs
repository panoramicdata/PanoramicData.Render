namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class UnequalColumnWidthTests
{
	[Fact]
	public void Paginate_UsesExplicitUnequalColumnWidthsForPlacements()
	{
		var section = new SectionInfo
		{
			ColumnCount = 2,
			ColumnsEqualWidth = false,
			Columns =
			[
				new SectionColumnDefinition(3000, 360),
				new SectionColumnDefinition(6000, 0)
			]
		};
		var blocks = new[]
		{
			MakeBlock(12000f),
			MakeBlock(1000f),
		};

		var result = PageBuilder.Paginate(blocks, section);

		result.Should().ContainSingle();
		result[0].BlockPlacements.Should().HaveCount(2);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[0].XTwips.Should().Be(1440f);
		result[0].BlockPlacements[0].ContentWidthTwips.Should().Be(3000f);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);
		result[0].BlockPlacements[1].XTwips.Should().Be(4800f);
		result[0].BlockPlacements[1].ContentWidthTwips.Should().Be(6000f);
	}

	[Fact]
	public void Paginate_UsesRemainingWidthForUnspecifiedExplicitColumns()
	{
		var section = new SectionInfo
		{
			ColumnCount = 3,
			ColumnsEqualWidth = false,
			ColumnSpacingTwips = 240,
			Columns =
			[
				new SectionColumnDefinition(2000, 360),
				new SectionColumnDefinition(0, 240)
			]
		};
		var blocks = new[]
		{
			MakeBlock(12000f),
			MakeBlock(12000f),
			MakeBlock(1000f),
		};

		var result = PageBuilder.Paginate(blocks, section);

		result.Should().ContainSingle();
		result[0].BlockPlacements.Should().HaveCount(3);
		result[0].BlockPlacements[0].ContentWidthTwips.Should().Be(2000f);
		result[0].BlockPlacements[1].ContentWidthTwips.Should().Be(3380f);
		result[0].BlockPlacements[2].ContentWidthTwips.Should().Be(3380f);
		result[0].BlockPlacements[1].XTwips.Should().Be(3800f);
		result[0].BlockPlacements[2].XTwips.Should().Be(7420f);
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}
}