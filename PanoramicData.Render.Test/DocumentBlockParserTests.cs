namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class DocumentBlockParserTests
{
	[Fact]
	public void Parse_SingleParagraph_ReturnsParagraphBlock()
	{
		AssertParsed(TestDocxBuilder.CreateMinimalDocx(), blocks => blocks.Should().ContainSingle().Which.Should().BeOfType<ParagraphBlock>());
	}

	[Fact]
	public void Parse_ParagraphWithStyleId_CapturesStyleId()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithStyledParagraph("Heading1"), blocks =>
		{
			var para = blocks.Should().ContainSingle()
				.Which.Should().BeOfType<ParagraphBlock>().Subject;
			para.StyleId.Should().Be("Heading1");
		});
	}

	[Fact]
	public void Parse_ParagraphWithoutStyle_HasNullStyleId()
	{
		AssertParsed(TestDocxBuilder.CreateMinimalDocx(), blocks => FirstParagraph(blocks).StyleId.Should().BeNull());
	}

	[Fact]
	public void Parse_ParagraphWithSectionBreak_ReturnsParagraphThenSectionBreak()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithMultipleSections(), blocks => AssertBlockTypes(blocks, typeof(ParagraphBlock), typeof(SectionBreakBlock), typeof(ParagraphBlock)));
	}

	[Fact]
	public void Parse_SectionBreakBlock_ContainsSectionInfo()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithMultipleSections(), blocks =>
		{
			var sectionBreak = blocks[1].Should().BeOfType<SectionBreakBlock>().Subject;
			sectionBreak.SectionInfo.Should().NotBeNull();
			sectionBreak.SectionInfo.PageWidth.Should().Be(16838);
			sectionBreak.SectionInfo.Orientation.Should().Be(PageOrientation.Landscape);
		});
	}

	[Fact]
	public void Parse_TableElement_ReturnsTablePlaceholderBlock()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithTable(), blocks => AssertBlockTypes(blocks, typeof(TablePlaceholderBlock), typeof(ParagraphBlock)));
	}

	[Fact]
	public void Parse_TablePlaceholderBlock_ContainsTableElement()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithTable(), blocks => blocks[0].Should().BeOfType<TablePlaceholderBlock>().Subject.TableElement.Should().NotBeNull());
	}

	[Fact]
	public void Parse_MultipleParagraphs_ReturnsAll()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithParagraphs(5), blocks =>
		{
			blocks.Should().HaveCount(5);
			blocks.Should().AllSatisfy(b => b.Should().BeOfType<ParagraphBlock>());
		});
	}

	[Fact]
	public void Parse_ParagraphBlock_CapturesSourceElement()
	{
		AssertParsed(TestDocxBuilder.CreateMinimalDocx(), blocks => FirstParagraph(blocks).SourceElement.Should().NotBeNull());
	}

	[Fact]
	public void Parse_MixedContent_PreservesOrder()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithMixedContent(), blocks => AssertBlockTypes(blocks, typeof(ParagraphBlock), typeof(TablePlaceholderBlock), typeof(ParagraphBlock), typeof(ParagraphBlock)));
	}

	[Fact]
	public void Parse_EmptyBody_ReturnsEmptyList()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithEmptyBody(), blocks => blocks.Should().BeEmpty());
	}

	[Fact]
	public void Parse_NullBody_ThrowsArgumentNullException()
	{
		Action act = () => DocumentBlockParser.Parse(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Parse_ParagraphWithNumberingProperties_CapturesNumberingInfo()
	{
		AssertParsed(TestDocxBuilder.CreateDocxWithNumberedParagraph(numId: 1, ilvl: 0), blocks =>
		{
			var para = FirstParagraph(blocks);
			para.NumberingId.Should().Be(1);
			para.NumberingLevel.Should().Be(0);
		});
	}

	[Fact]
	public void Parse_ParagraphWithoutNumbering_HasNullNumberingInfo()
	{
		AssertParsed(TestDocxBuilder.CreateMinimalDocx(), blocks =>
		{
			var para = FirstParagraph(blocks);
			para.NumberingId.Should().BeNull();
			para.NumberingLevel.Should().BeNull();
		});
	}

	[Fact]
	public void Parse_ParagraphWithBiDi_SetsIsBiDi()
	{
		ParseParagraph(new ParagraphProperties(new BiDi()), "مرحبا").IsBiDi.Should().BeTrue();
	}

	[Fact]
	public void Parse_ParagraphWithoutBiDi_IsBiDiIsFalse()
	{
		ParseParagraph(new ParagraphProperties(), "Hello").IsBiDi.Should().BeFalse();
	}

	[Fact]
	public void Parse_ParagraphWithJustificationCenter_SetsAlignment()
	{
		ParseParagraph(JustifiedProperties(JustificationValues.Center), "Centered").Alignment.Should().Be(ParagraphAlignment.Center);
	}

	[Fact]
	public void Parse_ParagraphWithJustificationRight_SetsAlignment()
	{
		ParseParagraph(JustifiedProperties(JustificationValues.Right), "Right").Alignment.Should().Be(ParagraphAlignment.Right);
	}

	[Fact]
	public void Parse_ParagraphWithNoJustification_AlignmentIsNull()
	{
		ParseParagraph(new ParagraphProperties(), "Default").Alignment.Should().BeNull();
	}

	[Fact]
	public void Parse_SdtBlockWithParagraph_UnwrapsContentIntoParagraphBlock()
	{
		var blocks = ParseSdt(SdtParagraph("SDT content"));

		blocks.Should().ContainSingle();
		FirstParagraph(blocks).SourceElement.InnerText.Should().Be("SDT content");
	}

	[Fact]
	public void Parse_SdtBlockWithMultipleParagraphs_UnwrapsAll()
	{
		var blocks = ParseSdt(SdtParagraph("First"), SdtParagraph("Second"));

		AssertBlockTypes(blocks, typeof(ParagraphBlock), typeof(ParagraphBlock));
	}

	[Fact]
	public void Parse_SdtBlockWithTable_UnwrapsTable()
	{
		var table = new Table(
			new TableProperties(),
			new TableGrid(new GridColumn { Width = "2000" }),
			new TableRow(new TableCell(SdtParagraph("Cell"))));

		var blocks = ParseSdt(table);

		blocks.Should().ContainSingle();
		blocks[0].Should().BeOfType<TablePlaceholderBlock>();
	}

	[Fact]
	public void Parse_NestedSdtBlocks_UnwrapsRecursively()
	{
		var innerSdt = new SdtBlock(new SdtProperties(), new SdtContentBlock(SdtParagraph("Nested")));

		var blocks = ParseSdt(innerSdt);

		blocks.Should().ContainSingle();
		FirstParagraph(blocks).SourceElement.InnerText.Should().Be("Nested");
	}

	[Fact]
	public void Parse_ParagraphWithNumIdZero_TreatsAsNoNumbering()
	{
		// Per OOXML spec, numId=0 means "suppress inherited list numbering".
		// It must not be treated as an active numbering sequence.
		var properties = new ParagraphProperties(
			new NumberingProperties(
				new NumberingLevelReference { Val = 0 },
				new NumberingId { Val = 0 }));
		var para = ParseParagraph(properties, "Not numbered");
		para.NumberingId.Should().BeNull();
		para.NumberingLevel.Should().BeNull();
	}

	private static void AssertParsed(MemoryStream stream, Action<IReadOnlyList<DocumentBlock>> assertBlocks)
	{
		using (stream)
		{
			using var doc = DocxDocument.Load(stream);
			assertBlocks(DocumentBlockParser.Parse(doc.DocumentBody));
		}
	}

	private static void AssertBlockTypes(IReadOnlyList<DocumentBlock> blocks, params Type[] expectedTypes)
	{
		blocks.Should().HaveCount(expectedTypes.Length);
		for (var index = 0; index < expectedTypes.Length; index++)
		{
			blocks[index].Should().BeOfType(expectedTypes[index]);
		}
	}

	private static ParagraphBlock FirstParagraph(IReadOnlyList<DocumentBlock> blocks)
		=> blocks[0].Should().BeOfType<ParagraphBlock>().Subject;

	private static ParagraphProperties JustifiedProperties(JustificationValues value)
		=> new(new Justification { Val = value });

	private static Paragraph SdtParagraph(string text) => new(new Run(new Text(text)));

	private static IReadOnlyList<DocumentBlock> ParseSdt(params OpenXmlElement[] content)
		=> DocumentBlockParser.Parse(new Body(new SdtBlock(new SdtProperties(), new SdtContentBlock(content))));

	private static ParagraphBlock ParseParagraph(ParagraphProperties properties, string text)
	{
		var blocks = DocumentBlockParser.Parse(new Body(new Paragraph(properties, new Run(new Text(text)))));
		return FirstParagraph(blocks);
	}
}
