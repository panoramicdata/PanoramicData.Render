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
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		blocks.Should().ContainSingle()
			.Which.Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_ParagraphWithStyleId_CapturesStyleId()
	{
		using var stream = TestDocxBuilder.CreateDocxWithStyledParagraph("Heading1");
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks.Should().ContainSingle()
			.Which.Should().BeOfType<ParagraphBlock>().Subject;
		para.StyleId.Should().Be("Heading1");
	}

	[Fact]
	public void Parse_ParagraphWithoutStyle_HasNullStyleId()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.StyleId.Should().BeNull();
	}

	[Fact]
	public void Parse_ParagraphWithSectionBreak_ReturnsParagraphThenSectionBreak()
	{
		using var stream = TestDocxBuilder.CreateDocxWithMultipleSections();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		// First paragraph has a section break in its properties
		blocks.Should().HaveCount(3);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<SectionBreakBlock>();
		blocks[2].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_SectionBreakBlock_ContainsSectionInfo()
	{
		using var stream = TestDocxBuilder.CreateDocxWithMultipleSections();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var sectionBreak = blocks[1].Should().BeOfType<SectionBreakBlock>().Subject;
		sectionBreak.SectionInfo.Should().NotBeNull();
		sectionBreak.SectionInfo.PageWidth.Should().Be(16838);
		sectionBreak.SectionInfo.Orientation.Should().Be(PageOrientation.Landscape);
	}

	[Fact]
	public void Parse_TableElement_ReturnsTablePlaceholderBlock()
	{
		using var stream = TestDocxBuilder.CreateDocxWithTable();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		blocks.Should().HaveCount(2);
		blocks[0].Should().BeOfType<TablePlaceholderBlock>();
		blocks[1].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_TablePlaceholderBlock_ContainsTableElement()
	{
		using var stream = TestDocxBuilder.CreateDocxWithTable();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var table = blocks[0].Should().BeOfType<TablePlaceholderBlock>().Subject;
		table.TableElement.Should().NotBeNull();
	}

	[Fact]
	public void Parse_MultipleParagraphs_ReturnsAll()
	{
		using var stream = TestDocxBuilder.CreateDocxWithParagraphs(5);
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		blocks.Should().HaveCount(5);
		blocks.Should().AllSatisfy(b => b.Should().BeOfType<ParagraphBlock>());
	}

	[Fact]
	public void Parse_ParagraphBlock_CapturesSourceElement()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.SourceElement.Should().NotBeNull();
	}

	[Fact]
	public void Parse_MixedContent_PreservesOrder()
	{
		using var stream = TestDocxBuilder.CreateDocxWithMixedContent();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		blocks.Should().HaveCount(4);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<TablePlaceholderBlock>();
		blocks[2].Should().BeOfType<ParagraphBlock>();
		blocks[3].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_EmptyBody_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateDocxWithEmptyBody();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		blocks.Should().BeEmpty();
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
		using var stream = TestDocxBuilder.CreateDocxWithNumberedParagraph(numId: 1, ilvl: 0);
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.NumberingId.Should().Be(1);
		para.NumberingLevel.Should().Be(0);
	}

	[Fact]
	public void Parse_ParagraphWithoutNumbering_HasNullNumberingInfo()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.NumberingId.Should().BeNull();
		para.NumberingLevel.Should().BeNull();
	}

	[Fact]
	public void Parse_ParagraphWithBiDi_SetsIsBiDi()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi()),
			new Run(new Text("مرحبا")));
		var body = new Body(paragraph);

		var blocks = DocumentBlockParser.Parse(body);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.IsBiDi.Should().BeTrue();
	}

	[Fact]
	public void Parse_ParagraphWithoutBiDi_IsBiDiIsFalse()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(),
			new Run(new Text("Hello")));
		var body = new Body(paragraph);

		var blocks = DocumentBlockParser.Parse(body);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.IsBiDi.Should().BeFalse();
	}

	[Fact]
	public void Parse_ParagraphWithJustificationCenter_SetsAlignment()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
			new Run(new Text("Centered")));
		var body = new Body(paragraph);

		var blocks = DocumentBlockParser.Parse(body);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.Alignment.Should().Be(ParagraphAlignment.Center);
	}

	[Fact]
	public void Parse_ParagraphWithJustificationRight_SetsAlignment()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
			new Run(new Text("Right")));
		var body = new Body(paragraph);

		var blocks = DocumentBlockParser.Parse(body);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.Alignment.Should().Be(ParagraphAlignment.Right);
	}

	[Fact]
	public void Parse_ParagraphWithNoJustification_AlignmentIsNull()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(),
			new Run(new Text("Default")));
		var body = new Body(paragraph);

		var blocks = DocumentBlockParser.Parse(body);

		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.Alignment.Should().BeNull();
	}

	[Fact]
	public void Parse_SdtBlockWithParagraph_UnwrapsContentIntoParagraphBlock()
	{
		var sdt = new SdtBlock(
			new SdtProperties(),
			new SdtContentBlock(
				new Paragraph(new Run(new Text("SDT content")))));
		var body = new Body(sdt);

		var blocks = DocumentBlockParser.Parse(body);

		blocks.Should().ContainSingle();
		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.SourceElement.InnerText.Should().Be("SDT content");
	}

	[Fact]
	public void Parse_SdtBlockWithMultipleParagraphs_UnwrapsAll()
	{
		var sdt = new SdtBlock(
			new SdtProperties(),
			new SdtContentBlock(
				new Paragraph(new Run(new Text("First"))),
				new Paragraph(new Run(new Text("Second")))));
		var body = new Body(sdt);

		var blocks = DocumentBlockParser.Parse(body);

		blocks.Should().HaveCount(2);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_SdtBlockWithTable_UnwrapsTable()
	{
		var table = new Table(
			new TableProperties(),
			new TableGrid(new GridColumn { Width = "2000" }),
			new TableRow(new TableCell(new Paragraph(new Run(new Text("Cell"))))));
		var sdt = new SdtBlock(
			new SdtProperties(),
			new SdtContentBlock(table));
		var body = new Body(sdt);

		var blocks = DocumentBlockParser.Parse(body);

		blocks.Should().ContainSingle();
		blocks[0].Should().BeOfType<TablePlaceholderBlock>();
	}

	[Fact]
	public void Parse_NestedSdtBlocks_UnwrapsRecursively()
	{
		var innerSdt = new SdtBlock(
			new SdtProperties(),
			new SdtContentBlock(
				new Paragraph(new Run(new Text("Nested")))));
		var outerSdt = new SdtBlock(
			new SdtProperties(),
			new SdtContentBlock(innerSdt));
		var body = new Body(outerSdt);

		var blocks = DocumentBlockParser.Parse(body);

		blocks.Should().ContainSingle();
		var para = blocks[0].Should().BeOfType<ParagraphBlock>().Subject;
		para.SourceElement.InnerText.Should().Be("Nested");
	}
}
