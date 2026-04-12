namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class FieldUpdateEngineTests
{
	[Fact]
	public void Apply_PageAndNumPagesFields_UpdatesCachedResultText()
	{
		using var stream = CreateDocxWithPageFieldParagraphs();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain(["PAGE", "NUMPAGES"]);

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1");
		GetParagraphResultText(paragraphs[1]).Should().Be("2");
		GetParagraphResultText(paragraphs[2]).Should().Be("2");
	}

	[Fact]
	public void Apply_DocumentPropertyFields_UpdatesCachedResultText()
	{
		using var stream = CreateDocxWithDocumentPropertyFieldParagraphs();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions(),
			SourceFilename = "uploaded.docx"
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain(["AUTHOR", "DESCRIPTION", "FILENAME", "KEYWORDS", "SUBJECT", "TITLE"]);

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("Quarterly Report");
		GetParagraphResultText(paragraphs[1]).Should().Be("Alice Example");
		GetParagraphResultText(paragraphs[2]).Should().Be("Master Services Agreement");
		GetParagraphResultText(paragraphs[3]).Should().Be("finance; forecast");
		GetParagraphResultText(paragraphs[4]).Should().Be("Uploaded by browser client");
		GetParagraphResultText(paragraphs[5]).Should().Be("uploaded.docx");
	}

	[Fact]
	public void Apply_TableOfContents_RebuildsEntryParagraphs()
	{
		using var stream = CreateDocxWithTableOfContentsField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(5);
		GetParagraphStyleId(paragraphs[1]).Should().Be("TOC1");
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One\t2");
		GetParagraphStyleId(paragraphs[2]).Should().Be("TOC1");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two\t3");
		paragraphs.Select(GetParagraphResultText).Should().NotContain("Old Entry\t99");
	}

	[Fact]
	public void Apply_TableOfContents_WithCustomSeparator_OmitsDefaultTabLeader()
	{
		using var stream = CreateDocxWithTableOfContentsField("TOC \\o \"1-3\" \\p \" -- \"");
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One -- 2");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two -- 3");
	}

	[Fact]
	public void Apply_TableOfContents_WithNoPageNumbersSwitch_OmitsPageNumbers()
	{
		using var stream = CreateDocxWithTableOfContentsField("TOC \\o \"1-3\" \\n");
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two");
	}

	[Fact]
	public void Apply_TableOfContents_WithCustomStyleSwitch_IncludesMappedParagraphs()
	{
		using var stream = CreateDocxWithCustomStyleTableOfContentsField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(3);
		GetParagraphStyleId(paragraphs[1]).Should().Be("TOC2");
		GetParagraphResultText(paragraphs[1]).Should().Be("Appendix A\t2");
	}

	[Fact]
	public void Apply_TableOfContents_WithHyperlinkSwitch_WrapsEntriesInBookmarkHyperlinks()
	{
		using var stream = CreateDocxWithHyperlinkedTableOfContentsField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		var firstEntryLink = paragraphs[1].Descendants<Hyperlink>().Should().ContainSingle().Subject;
		firstEntryLink.Anchor?.Value.Should().Be("_TocChapterOne");
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One\t2");

		var secondEntryLink = paragraphs[2].Descendants<Hyperlink>().Should().ContainSingle().Subject;
		secondEntryLink.Anchor?.Value.Should().Be("_TocChapterTwo");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two\t3");
	}

	[Fact]
	public void Apply_TableOfContents_WithHyperlinkSwitch_GeneratesSyntheticBookmarksForHeadingsWithoutAnchors()
	{
		using var stream = CreateDocxWithHyperlinkedTableOfContentsFieldWithoutHeadingBookmarks();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		var firstSyntheticAnchor = GetParagraphHyperlinkAnchor(paragraphs[1]);
		firstSyntheticAnchor.Should().Be("_TocGenerated1");
		GetHeadingBookmarkName(paragraphs[3]).Should().Be(firstSyntheticAnchor);

		var secondSyntheticAnchor = GetParagraphHyperlinkAnchor(paragraphs[2]);
		secondSyntheticAnchor.Should().Be("_TocGenerated2");
		GetHeadingBookmarkName(paragraphs[4]).Should().Be(secondSyntheticAnchor);
	}

	internal static MemoryStream CreateDocxWithPageFieldParagraphs()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("PAGE", "999"),
					CreateComplexFieldParagraph("PAGE", "999", pageBreakBefore: true),
					CreateComplexFieldParagraph("NUMPAGES", "999")));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithDocumentPropertyFieldParagraphs()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			document.PackageProperties.Title = "Quarterly Report";
			document.PackageProperties.Creator = "Alice Example";
			document.PackageProperties.Subject = "Master Services Agreement";
			document.PackageProperties.Keywords = "finance; forecast";
			document.PackageProperties.Description = "Uploaded by browser client";

			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TITLE", "Old Title"),
					CreateComplexFieldParagraph("AUTHOR", "Old Author"),
					CreateComplexFieldParagraph("SUBJECT", "Old Subject"),
					CreateComplexFieldParagraph("KEYWORDS", "Old Keywords"),
					CreateComplexFieldParagraph("DESCRIPTION", "Old Description"),
					CreateComplexFieldParagraph("FILENAME", "Old File")));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithTableOfContentsField(string instruction = "TOC \\o \"1-3\"")
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph(instruction, string.Empty),
					CreateStyledParagraph("TOC1", "Old Entry\t99"),
					CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
					CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithCustomStyleTableOfContentsField()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles(
				new Style(
					new StyleName { Val = "Custom Heading" },
					new PrimaryStyle())
				{
					Type = StyleValues.Paragraph,
					StyleId = "CustomHeading"
				});

			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\t \"Custom Heading,2\"", string.Empty),
					CreateStyledParagraph("TOC1", "Old Entry\t99"),
					CreateStyledParagraph("CustomHeading", "Appendix A", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithHyperlinkedTableOfContentsField()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\o \"1-3\" \\h", string.Empty),
					CreateStyledParagraph("TOC1", "Old Entry\t99"),
					CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true, bookmarkName: "_TocChapterOne", bookmarkId: 1),
					CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true, bookmarkName: "_TocChapterTwo", bookmarkId: 2)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithHyperlinkedTableOfContentsFieldWithoutHeadingBookmarks()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\o \"1-3\" \\h", string.Empty),
					CreateStyledParagraph("TOC1", "Old Entry\t99"),
					CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
					CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	private static Paragraph CreateComplexFieldParagraph(string instruction, string cachedValue, bool pageBreakBefore = false)
	{
		var paragraph = new Paragraph();
		if (pageBreakBefore)
		{
			paragraph.AppendChild(new ParagraphProperties(new PageBreakBefore()));
		}

		paragraph.Append(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = $" {instruction} " }),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedValue)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

		return paragraph;
	}

	private static Paragraph CreateStyledParagraph(
		string styleId,
		string text,
		bool pageBreakBefore = false,
		string? bookmarkName = null,
		int bookmarkId = 0)
	{
		var properties = new ParagraphProperties(new ParagraphStyleId { Val = styleId });
		if (pageBreakBefore)
		{
			properties.AppendChild(new PageBreakBefore());
		}

		var paragraph = new Paragraph(properties);
		if (!string.IsNullOrWhiteSpace(bookmarkName))
		{
			paragraph.AppendChild(new BookmarkStart
			{
				Id = bookmarkId.ToString(System.Globalization.CultureInfo.InvariantCulture),
				Name = bookmarkName
			});
		}

		paragraph.AppendChild(new Run(new Text(text)));

		if (!string.IsNullOrWhiteSpace(bookmarkName))
		{
			paragraph.AppendChild(new BookmarkEnd
			{
				Id = bookmarkId.ToString(System.Globalization.CultureInfo.InvariantCulture)
			});
		}

		return paragraph;
	}

	private static string GetParagraphResultText(Paragraph paragraph)
		=> string.Concat(paragraph.Descendants<Text>().Select(text => text.Text));

	private static string? GetParagraphStyleId(Paragraph paragraph)
		=> paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

	private static string? GetParagraphHyperlinkAnchor(Paragraph paragraph)
		=> paragraph.Descendants<Hyperlink>().FirstOrDefault()?.Anchor?.Value;

	private static string? GetHeadingBookmarkName(Paragraph paragraph)
		=> paragraph.Elements<BookmarkStart>().FirstOrDefault()?.Name?.Value;
}