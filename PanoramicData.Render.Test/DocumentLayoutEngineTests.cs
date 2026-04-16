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
	public void MeasureBlocks_AdjacentParagraphSpacing_CollapsesBetweenParagraphs()
	{
		var first = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new ParagraphProperties(new SpacingBetweenLines { After = "300" }))
		};
		var second = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new ParagraphProperties(new SpacingBetweenLines { Before = "200" }))
		};

		var result = DocumentLayoutEngine.MeasureBlocks([first, second], naturalLineHeight: 240f);

		result.Should().HaveCount(2);
		result[0].SpaceAfter.Should().Be(300f);
		result[1].SpaceBefore.Should().Be(0f);
		result[1].HeightTwips.Should().Be(240f);
	}

	[Fact]
	public void MeasureBlocks_WithBodySectionInfo_WrapsSimpleParagraphAcrossMultipleLines()
	{
		var para = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Alpha Beta")))
		};
		var section = new SectionInfo
		{
			MarginLeft = 100,
			MarginRight = 100,
			PageWidth = 900
		};

		var result = DocumentLayoutEngine.MeasureBlocks([para], section);

		result.Should().ContainSingle();
		result[0].LineHeights.Should().HaveCount(2);
		result[0].HeightTwips.Should().BeGreaterThan(360f);
	}

	[Fact]
	public void MeasureBlocks_ParagraphWithRunFontSize_UsesRunFontSizeForLineHeight()
	{
		// A run with w:sz="22" (11pt) should produce a line height of 220 twips (single spacing).
		var run = new Run(new Text("Hello"))
		{
			RunProperties = new RunProperties(new FontSize { Val = "22" })
		};
		var para = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new ParagraphProperties(new SpacingBetweenLines { Line = "240", LineRule = LineSpacingRuleValues.Auto }),
				run)
		};

		var result = DocumentLayoutEngine.MeasureBlocks([para]);

		// With 11pt (220 twips) single spacing and no before/after, height ≈ 220 twips.
		// It must be significantly less than the heading-sized default of 360 twips.
		result[0].HeightTwips.Should().BeLessThan(300f);
		result[0].HeightTwips.Should().BeGreaterThan(180f);
	}

	[Fact]
	public void MeasureBlocks_ParagraphWithLargeRunFontSize_UsesRunFontSizeForLineHeight()
	{
		// A run with w:sz="36" (18pt) should produce a line height near 360 twips.
		var run = new Run(new Text("Heading"))
		{
			RunProperties = new RunProperties(new FontSize { Val = "36" })
		};
		var para = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new ParagraphProperties(new SpacingBetweenLines { Line = "240", LineRule = LineSpacingRuleValues.Auto }),
				run)
		};

		var result = DocumentLayoutEngine.MeasureBlocks([para]);

		// 18pt = 360 twips single spacing. Must be clearly larger than body-text range.
		result[0].HeightTwips.Should().BeGreaterThanOrEqualTo(340f);
		result[0].HeightTwips.Should().BeLessThanOrEqualTo(380f);
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

	// ===================================================================
	// ExtractDocDefaultSpacing
	// ===================================================================

	[Fact]
	public void ExtractDocDefaultSpacing_NullStyles_ReturnsParagraphSpacingNone()
	{
		var result = DocumentLayoutEngine.ExtractDocDefaultSpacing(null);

		result.SpaceBefore.Should().Be(0f);
		result.SpaceAfter.Should().Be(0f);
		result.LineSpacingTwips.Should().Be(0f);
		result.LineRule.Should().BeNull();
	}

	[Fact]
	public void ExtractDocDefaultSpacing_StylesWithNoDocDefaults_ReturnsParagraphSpacingNone()
	{
		var styles = new Styles();
		var result = DocumentLayoutEngine.ExtractDocDefaultSpacing(styles);

		result.SpaceBefore.Should().Be(0f);
		result.SpaceAfter.Should().Be(0f);
	}

	[Fact]
	public void ExtractDocDefaultSpacing_WithAfterAndLineValues_ReturnsCorrectSpacing()
	{
		// Matches panoramic-data-document-2026 docDefaults: after=160, line=259, lineRule=auto
		var styles = new Styles(
			new DocDefaults(
				new ParagraphPropertiesDefault(
					new ParagraphPropertiesBaseStyle(
						new SpacingBetweenLines
						{
							After = "160",
							Line = "259",
							LineRule = LineSpacingRuleValues.Auto
						}))));

		var result = DocumentLayoutEngine.ExtractDocDefaultSpacing(styles);

		result.SpaceAfter.Should().Be(160f);
		result.LineSpacingTwips.Should().Be(259f);
		result.LineRule.Should().BeNull(); // Auto is represented as null
	}

	[Fact]
	public void MeasureBlocks_WithDocDefaultSpacingViaStyles_AppliesAfterSpacingToParagraph()
	{
		// A paragraph with no explicit spacing should inherit docDefault after=160
		var styles = new Styles(
			new DocDefaults(
				new ParagraphPropertiesDefault(
					new ParagraphPropertiesBaseStyle(
						new SpacingBetweenLines { After = "160" }))));

		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var sectionInfo = new SectionInfo();
		var result = DocumentLayoutEngine.MeasureBlocks([para], sectionInfo, styles);

		// SpaceAfter should be the docDefault 160 twips
		result[0].SpaceAfter.Should().Be(160f);
	}

	/// <summary>
	/// A test-only <see cref="DocumentBlock"/> subclass for exercising the default measurement path.
	/// </summary>
	private sealed class TestDocumentBlock : DocumentBlock;
}
