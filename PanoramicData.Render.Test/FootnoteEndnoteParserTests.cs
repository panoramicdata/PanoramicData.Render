namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class FootnoteEndnoteParserTests
{
	[Fact]
	public void ParseFootnotes_WithSingleDefinition_ReturnsDefinitions()
	{
		using var stream = TestDocxBuilder.CreateDocxWithSingleFootnote(1, "Footnote one");
		using var doc = DocxDocument.Load(stream);

		var footnotes = FootnoteEndnoteParser.ParseFootnotes(doc.MainDocumentPart);

		footnotes.Should().HaveCount(2);
		footnotes[0].Id.Should().Be(-1);
		footnotes[0].Type.Should().Be(FootnoteEndnoteValues.Separator);
		footnotes[1].Id.Should().Be(1);
		footnotes[1].Type.Should().BeNull();
		footnotes[1].Blocks.Should().HaveCount(1);
		footnotes[1].Blocks[0].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void ParseFootnotes_WithoutPart_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var footnotes = FootnoteEndnoteParser.ParseFootnotes(doc.MainDocumentPart);

		footnotes.Should().BeEmpty();
	}

	[Fact]
	public void ParseFootnotes_NullMainPart_ThrowsArgumentNullException()
	{
		var act = () => FootnoteEndnoteParser.ParseFootnotes(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseFootnotes_FootnoteContentIsAccessible()
	{
		using var stream = TestDocxBuilder.CreateDocxWithSingleFootnote(3, "Custom note text");
		using var doc = DocxDocument.Load(stream);

		var footnotes = FootnoteEndnoteParser.ParseFootnotes(doc.MainDocumentPart);

		var note = footnotes.Should().ContainSingle(n => n.Id == 3).Subject;
		var paragraph = note.Blocks.Should().ContainSingle().Subject.Should().BeOfType<ParagraphBlock>().Subject;
		paragraph.SourceElement.InnerText.Should().Be("Custom note text");
	}

	[Fact]
	public void ParseFootnotes_FootnoteWithTable_ContainsTablePlaceholder()
	{
		using var stream = TestDocxBuilder.CreateDocxWithFootnoteContainingTable(2);
		using var doc = DocxDocument.Load(stream);

		var footnotes = FootnoteEndnoteParser.ParseFootnotes(doc.MainDocumentPart);

		var note = footnotes.Should().ContainSingle(n => n.Id == 2).Subject;
		note.Blocks.Should().HaveCount(3);
		note.Blocks[0].Should().BeOfType<ParagraphBlock>();
		note.Blocks[1].Should().BeOfType<TablePlaceholderBlock>();
		note.Blocks[2].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void ParseFootnotes_WhenPartHasNoRootElement_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateDocxWithFootnotesPartWithoutRoot();
		using var doc = DocxDocument.Load(stream);

		var footnotes = FootnoteEndnoteParser.ParseFootnotes(doc.MainDocumentPart);

		footnotes.Should().BeEmpty();
	}

	[Fact]
	public void ParseEndnotes_WithSingleDefinition_ReturnsDefinitions()
	{
		using var stream = TestDocxBuilder.CreateDocxWithSingleEndnote(4, "Endnote one");
		using var doc = DocxDocument.Load(stream);

		var endnotes = FootnoteEndnoteParser.ParseEndnotes(doc.MainDocumentPart);

		endnotes.Should().HaveCount(2);
		endnotes[0].Id.Should().Be(-1);
		endnotes[0].Type.Should().Be(FootnoteEndnoteValues.Separator);
		endnotes[1].Id.Should().Be(4);
		endnotes[1].Type.Should().BeNull();
		endnotes[1].Blocks.Should().HaveCount(1);
		endnotes[1].Blocks[0].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void ParseEndnotes_WithoutPart_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var doc = DocxDocument.Load(stream);

		var endnotes = FootnoteEndnoteParser.ParseEndnotes(doc.MainDocumentPart);

		endnotes.Should().BeEmpty();
	}

	[Fact]
	public void ParseEndnotes_NullMainPart_ThrowsArgumentNullException()
	{
		var act = () => FootnoteEndnoteParser.ParseEndnotes(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseEndnotes_EndnoteContentIsAccessible()
	{
		using var stream = TestDocxBuilder.CreateDocxWithSingleEndnote(8, "Custom endnote");
		using var doc = DocxDocument.Load(stream);

		var endnotes = FootnoteEndnoteParser.ParseEndnotes(doc.MainDocumentPart);

		var note = endnotes.Should().ContainSingle(n => n.Id == 8).Subject;
		var paragraph = note.Blocks.Should().ContainSingle().Subject.Should().BeOfType<ParagraphBlock>().Subject;
		paragraph.SourceElement.InnerText.Should().Be("Custom endnote");
	}

	[Fact]
	public void ParseEndnotes_WhenPartHasNoRootElement_ReturnsEmptyList()
	{
		using var stream = TestDocxBuilder.CreateDocxWithEndnotesPartWithoutRoot();
		using var doc = DocxDocument.Load(stream);

		var endnotes = FootnoteEndnoteParser.ParseEndnotes(doc.MainDocumentPart);

		endnotes.Should().BeEmpty();
	}

	[Fact]
	public void ParseFootnotesAndEndnotes_WithBothParts_ReturnsIndependentCollections()
	{
		using var stream = TestDocxBuilder.CreateDocxWithFootnotesAndEndnotes();
		using var doc = DocxDocument.Load(stream);

		var footnotes = FootnoteEndnoteParser.ParseFootnotes(doc.MainDocumentPart);
		var endnotes = FootnoteEndnoteParser.ParseEndnotes(doc.MainDocumentPart);

		footnotes.Should().ContainSingle(n => n.Id == 5);
		endnotes.Should().ContainSingle(n => n.Id == 7);
	}
}
