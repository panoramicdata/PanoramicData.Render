namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Tests for parsing bookmark start/end elements from DOCX paragraphs.
/// Covers step 7.5.1.
/// </summary>
public sealed class BookmarkParsingTests
{
	[Fact]
	public void Parse_ParagraphWithBookmarkStartAndEnd_ExtractsBoth()
	{
		var paragraph = new Paragraph(
			new BookmarkStart { Id = "42", Name = "MyBookmark" },
			new Run(new Text("Hello")),
			new BookmarkEnd { Id = "42" });

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().ContainSingle();
		block.BookmarkStarts[0].Id.Should().Be(42);
		block.BookmarkStarts[0].Name.Should().Be("MyBookmark");

		block.BookmarkEnds.Should().ContainSingle();
		block.BookmarkEnds[0].Id.Should().Be(42);
	}

	[Fact]
	public void Parse_ParagraphWithMultipleBookmarkStarts_ExtractsAll()
	{
		var paragraph = new Paragraph(
			new BookmarkStart { Id = "0", Name = "_Toc123" },
			new BookmarkStart { Id = "1", Name = "_Ref456" },
			new Run(new Text("Text")),
			new BookmarkEnd { Id = "0" },
			new BookmarkEnd { Id = "1" });

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().HaveCount(2);
		block.BookmarkStarts[0].Id.Should().Be(0);
		block.BookmarkStarts[0].Name.Should().Be("_Toc123");
		block.BookmarkStarts[1].Id.Should().Be(1);
		block.BookmarkStarts[1].Name.Should().Be("_Ref456");

		block.BookmarkEnds.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_ParagraphWithOnlyBookmarkStart_ExtractsStartOnly()
	{
		// Bookmark starts here, ends in another paragraph
		var paragraph = new Paragraph(
			new BookmarkStart { Id = "5", Name = "CrossParagraph" },
			new Run(new Text("Start")));

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().ContainSingle();
		block.BookmarkStarts[0].Name.Should().Be("CrossParagraph");
		block.BookmarkEnds.Should().BeEmpty();
	}

	[Fact]
	public void Parse_ParagraphWithOnlyBookmarkEnd_ExtractsEndOnly()
	{
		// Bookmark started in previous paragraph, ends here
		var paragraph = new Paragraph(
			new Run(new Text("End")),
			new BookmarkEnd { Id = "5" });

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().BeEmpty();
		block.BookmarkEnds.Should().ContainSingle();
		block.BookmarkEnds[0].Id.Should().Be(5);
	}

	[Fact]
	public void Parse_ParagraphWithNoBookmarks_ReturnsEmptyLists()
	{
		var paragraph = new Paragraph(new Run(new Text("Plain text")));

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().BeEmpty();
		block.BookmarkEnds.Should().BeEmpty();
	}

	[Fact]
	public void Parse_BookmarkStartWithoutName_IsSkipped()
	{
		var paragraph = new Paragraph(
			new BookmarkStart { Id = "0" },
			new Run(new Text("No name")),
			new BookmarkEnd { Id = "0" });

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().BeEmpty();
		block.BookmarkEnds.Should().ContainSingle();
	}

	[Fact]
	public void Parse_BookmarkEndWithoutId_IsSkipped()
	{
		var paragraph = new Paragraph(
			new BookmarkStart { Id = "0", Name = "Valid" },
			new Run(new Text("Text")),
			new BookmarkEnd());

		var block = DocumentBlockParser.CreateParagraphBlock(paragraph);

		block.BookmarkStarts.Should().ContainSingle();
		block.BookmarkEnds.Should().BeEmpty();
	}

	[Fact]
	public void Parse_FullDocxWithBookmarks_ExtractsAcrossParagraphs()
	{
		using var stream = TestDocxBuilder.CreateDocxWithBookmarks();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		// 3 paragraphs
		blocks.Should().HaveCount(3);

		// Paragraph 1: bookmark start+end (id=0, name="Introduction")
		var p1 = blocks[0].Should().BeOfType<ParagraphBlock>().Which;
		p1.BookmarkStarts.Should().ContainSingle();
		p1.BookmarkStarts[0].Id.Should().Be(0);
		p1.BookmarkStarts[0].Name.Should().Be("Introduction");
		p1.BookmarkEnds.Should().ContainSingle();
		p1.BookmarkEnds[0].Id.Should().Be(0);

		// Paragraph 2: bookmark start only (id=1, name="Chapter1")
		var p2 = blocks[1].Should().BeOfType<ParagraphBlock>().Which;
		p2.BookmarkStarts.Should().ContainSingle();
		p2.BookmarkStarts[0].Name.Should().Be("Chapter1");
		p2.BookmarkEnds.Should().BeEmpty();

		// Paragraph 3: bookmark end only (id=1)
		var p3 = blocks[2].Should().BeOfType<ParagraphBlock>().Which;
		p3.BookmarkStarts.Should().BeEmpty();
		p3.BookmarkEnds.Should().ContainSingle();
		p3.BookmarkEnds[0].Id.Should().Be(1);
	}

	[Fact]
	public void Parse_BookmarkStartWithoutName_DocxRoundTrip_IsSkipped()
	{
		using var stream = TestDocxBuilder.CreateDocxWithBookmarkNoName();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		blocks.Should().ContainSingle();
		var p = blocks[0].Should().BeOfType<ParagraphBlock>().Which;
		p.BookmarkStarts.Should().BeEmpty("bookmarkStart without a name is skipped");
		p.BookmarkEnds.Should().ContainSingle();
	}
}
