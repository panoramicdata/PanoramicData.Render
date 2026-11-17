namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class FieldUpdateEngineTests
{
	[Fact]
	public void Apply_PageRefField_ResolvesToBookmarkPageNumber()
	{
		using var run = ApplyFieldUpdate(CreateDocxWithPageRefField());
		var doc = run.Doc;
		var result = run.Result;

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("PAGEREF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("2");
	}

	[Fact]
	public void Apply_RefField_ResolvesToBookmarkText()
	{
		using var run = ApplyFieldUpdate(CreateDocxWithRefField());
		var doc = run.Doc;
		var result = run.Result;

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("REF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("Target Text");
	}

	[Fact]
	public void Apply_CrossReferenceFields_WhenDisabled_DoesNotUpdate()
	{
		using var stream = CreateDocxWithPageRefField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions { UpdateCrossReferences = false }
		});

		result.UpdatedFields.Should().NotContain("PAGEREF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("99");
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

	internal static MemoryStream CreateDocxWithTableOfFiguresField()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\f \"Figure\"", string.Empty),
					CreateStyledParagraph("TOC1", "Old Figure\t99"),
					CreateStyledParagraph("Caption", "Figure 1. Overview", pageBreakBefore: true),
					CreateStyledParagraph("Caption", "Figure 2. Details", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\f \"Figure\"", string.Empty),
					CreateStyledParagraph("TOC1", "Old Figure\t99"),
					CreateSeqFigureParagraph("99", ". Overview", pageBreakBefore: true),
					CreateSeqFigureParagraph("99", ". Details", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithTableOfContentsFieldRequiringThirdPassConvergence()
	{
		var bodyChildren = new List<OpenXmlElement>
		{
			CreateComplexFieldParagraph("TOC \\o \"1-3\"", string.Empty),
			CreateStyledParagraph("TOC1", "Old Entry\t99")
		};

		for (var chapterIndex = 1; chapterIndex <= 12; chapterIndex++)
		{
			for (var fillerIndex = 1; fillerIndex <= 18; fillerIndex++)
			{
				bodyChildren.Add(CreatePlainParagraph($"Filler {chapterIndex}-{fillerIndex}"));
			}

			bodyChildren.Add(CreateStyledParagraph("Heading1", $"Chapter {chapterIndex}"));
		}

		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(bodyChildren));
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

	internal static MemoryStream CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\o \"1-3\"", string.Empty),
					new Paragraph(
						new ParagraphProperties(
							new ParagraphStyleId { Val = "TOC1" },
							new Tabs(
								new DocumentFormat.OpenXml.Wordprocessing.TabStop
								{
									Val = TabStopValues.Right,
									Position = 9360,
									Leader = TabStopLeaderCharValues.Dot
								})),
						new Run(new Text("Old Entry") { Space = SpaceProcessingModeValues.Preserve }),
						new Run(new TabChar()),
						new Run(new Text("99") { Space = SpaceProcessingModeValues.Preserve })),
					CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
					CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\o \"1-3\"", string.Empty),
					new Paragraph(
						new ParagraphProperties(
							new ParagraphStyleId { Val = "TOC1" },
							new Tabs(
								new DocumentFormat.OpenXml.Wordprocessing.TabStop
								{
									Val = TabStopValues.Right,
									Position = 9360,
									Leader = TabStopLeaderCharValues.Dot
								})),
						new Run(
							new RunProperties(new Bold(), new FontSize { Val = "28" }),
							new Text("Old Entry") { Space = SpaceProcessingModeValues.Preserve }),
						new Run(
							new RunProperties(new Italic(), new FontSize { Val = "24" }),
							new TabChar()),
						new Run(
							new RunProperties(new Italic(), new FontSize { Val = "20" }),
							new Text("99") { Space = SpaceProcessingModeValues.Preserve })),
					CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
					CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithTableOfContentsFieldAndStyleDefinedRunFormatting()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles(
				new Style(
					new StyleName { Val = "TOC 1" },
					new StyleParagraphProperties(
						new Tabs(
							new DocumentFormat.OpenXml.Wordprocessing.TabStop
							{
								Val = TabStopValues.Right,
								Position = 9360,
								Leader = TabStopLeaderCharValues.Dot
							})),
					new StyleRunProperties(new Bold(), new FontSize { Val = "28" }))
				{
					Type = StyleValues.Paragraph,
					StyleId = "TOC1"
				});

			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("TOC \\o \"1-3\"", string.Empty),
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

	private static MemoryStream CreateDocxWithSequenceFields()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateSeqFieldParagraph("SEQ Figure", "X", " caption text"),
					CreateSeqFieldParagraph("SEQ Figure", "X", " more text"),
					CreateSeqFieldParagraph("SEQ Table", "X", " first table")));
		}

		stream.Position = 0;
		return stream;
	}

	private static MemoryStream CreateDocxWithSequenceFieldsAndResetSwitch()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateSeqFieldParagraph("SEQ Figure", "X", " first"),
					CreateSeqFieldParagraph("SEQ Figure", "X", " second"),
					CreateSeqFieldParagraph("SEQ Figure \\r 10", "X", " reset"),
					CreateSeqFieldParagraph("SEQ Figure", "X", " after reset")));
		}

		stream.Position = 0;
		return stream;
	}

	private static MemoryStream CreateDocxWithSequenceFieldsAndHiddenSwitch()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateSeqFieldParagraph("SEQ Figure", "X", " visible"),
					CreateSeqFieldParagraph("SEQ Figure \\h", "X", " hidden counter"),
					CreateSeqFieldParagraph("SEQ Figure", "X", " skipped two")));
		}

		stream.Position = 0;
		return stream;
	}

	private static Paragraph CreateSeqFieldParagraph(string instruction, string cachedValue, string trailingText)
	{
		var paragraph = new Paragraph();
		paragraph.Append(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = $" {instruction} " }),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedValue)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
			new Run(new Text(trailingText) { Space = SpaceProcessingModeValues.Preserve }));

		return paragraph;
	}

	internal static MemoryStream CreateDocxWithPageRefField()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("PAGEREF _RefTarget", "99"),
					CreateStyledParagraph("Heading1", "Target Heading", pageBreakBefore: true, bookmarkName: "_RefTarget", bookmarkId: 1)));
		}

		stream.Position = 0;
		return stream;
	}

	internal static MemoryStream CreateDocxWithRefField()
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			mainPart.Document = new Document(
				new Body(
					CreateComplexFieldParagraph("REF _RefBookmark", "old text"),
					CreateBookmarkedParagraph("_RefBookmark", 1, "Target Text")));
		}

		stream.Position = 0;
		return stream;
	}

	private static Paragraph CreateBookmarkedParagraph(string bookmarkName, int bookmarkId, string text)
	{
		var idValue = bookmarkId.ToString(System.Globalization.CultureInfo.InvariantCulture);
		return new Paragraph(
			new BookmarkStart { Id = idValue, Name = bookmarkName },
			new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }),
			new BookmarkEnd { Id = idValue });
	}

	private static Paragraph CreateSeqFigureParagraph(string cachedSequenceText, string trailingText, bool pageBreakBefore = false)
	{
		var paragraph = new Paragraph();
		if (pageBreakBefore)
		{
			paragraph.AppendChild(new ParagraphProperties(new PageBreakBefore()));
		}

		paragraph.Append(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = " SEQ Figure " }),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedSequenceText)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
			new Run(new Text(trailingText) { Space = SpaceProcessingModeValues.Preserve }));

		return paragraph;
	}

	private static Paragraph CreatePlainParagraph(string text)
		=> new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

	private static string GetParagraphResultText(Paragraph paragraph)
		=> string.Concat(paragraph.Descendants<Text>().Select(text => text.Text));

	private static string[] GetParagraphTextNodes(Paragraph paragraph)
		=> [.. paragraph.Descendants<Text>().Select(text => text.Text)];

	private static string? GetParagraphStyleId(Paragraph paragraph)
		=> paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

	private static string? GetParagraphHyperlinkAnchor(Paragraph paragraph)
		=> paragraph.Descendants<Hyperlink>().FirstOrDefault()?.Anchor?.Value;

	private static string? GetHeadingBookmarkName(Paragraph paragraph)
		=> paragraph.Elements<BookmarkStart>().FirstOrDefault()?.Name?.Value;
}
