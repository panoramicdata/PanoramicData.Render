namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Tests for <see cref="DocumentLayoutEngine"/>.
/// </summary>
public sealed class DocumentLayoutEngineTests
{
	[Fact]
	public void MeasureBlocks_NullBlocks_ThrowsArgumentNullException()
	{
		var act = () => DocumentLayoutEngine.MeasureBlocks(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MeasureBlocks_EmptyList_ReturnsEmpty()
	{
		var result = DocumentLayoutEngine.MeasureBlocks([]);
		result.Should().BeEmpty();
	}

	[Fact]
	public void MeasureBlocks_SingleParagraph_ReturnsOneBlock()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var result = DocumentLayoutEngine.MeasureBlocks([para]);

		result.Should().ContainSingle();
		result[0].Block.Should().BeSameAs(para);
		result[0].HeightTwips.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureBlocks_ParagraphWithPageBreak_SetsForcePageBreakBefore()
	{
		var para = new ParagraphBlock
		{
			SourceElement = new Paragraph(),
			PageBreakBefore = true
		};

		var result = DocumentLayoutEngine.MeasureBlocks([para]);

		result[0].ForcePageBreakBefore.Should().BeTrue();
	}

	[Fact]
	public void MeasureBlocks_SectionBreak_HasZeroHeight()
	{
		var sectionBreak = new SectionBreakBlock { SectionInfo = new SectionInfo() };
		var result = DocumentLayoutEngine.MeasureBlocks([sectionBreak]);

		result.Should().ContainSingle();
		result[0].HeightTwips.Should().Be(0f);
	}

	[Fact]
	public void MeasureBlocks_Table_EstimatesFromRowCount()
	{
		var table = new Table(
			new TableRow(new TableCell(new Paragraph())),
			new TableRow(new TableCell(new Paragraph())),
			new TableRow(new TableCell(new Paragraph())));

		var block = new TablePlaceholderBlock { TableElement = table };
		var result = DocumentLayoutEngine.MeasureBlocks([block]);

		result.Should().ContainSingle();
		result[0].HeightTwips.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureBlocks_MixedBlocks_ReturnsSameCount()
	{
		DocumentBlock[] blocks =
		[
			new ParagraphBlock { SourceElement = new Paragraph() },
			new SectionBreakBlock { SectionInfo = new SectionInfo() },
			new ParagraphBlock { SourceElement = new Paragraph() },
		];

		var result = DocumentLayoutEngine.MeasureBlocks(blocks);

		result.Should().HaveCount(3);
	}

	[Fact]
	public void MeasureBlocks_CustomLineHeight_AffectsResult()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };

		var defaultResult = DocumentLayoutEngine.MeasureBlocks([para]);
		var largerResult = DocumentLayoutEngine.MeasureBlocks([para], 480f);

		largerResult[0].HeightTwips.Should().BeGreaterThan(defaultResult[0].HeightTwips);
	}

	[Fact]
	public void MeasureBlocks_FootnoteSeparator_HasPositiveHeight()
	{
		var block = new FootnoteSeparatorBlock();
		var result = DocumentLayoutEngine.MeasureBlocks([block]);

		result.Should().ContainSingle();
		result[0].HeightTwips.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureBlocks_UnknownBlockType_FallsBackToDefaultHeight()
	{
		var block = new TestDocumentBlock();
		var result = DocumentLayoutEngine.MeasureBlocks([block]);

		result.Should().ContainSingle();
		result[0].HeightTwips.Should().BeGreaterThan(0);
	}

	/// <summary>
	/// A test-only <see cref="DocumentBlock"/> subclass for exercising the default measurement path.
	/// </summary>
	private sealed class TestDocumentBlock : DocumentBlock;
}
