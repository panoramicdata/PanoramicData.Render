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
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Chapter One", "2"]);
		GetParagraphStyleId(paragraphs[2]).Should().Be("TOC1");
		paragraphs[2].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["Chapter Two", "3"]);
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
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Appendix A", "2"]);
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
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Chapter One", "2"]);

		var secondEntryLink = paragraphs[2].Descendants<Hyperlink>().Should().ContainSingle().Subject;
		secondEntryLink.Anchor?.Value.Should().Be("_TocChapterTwo");
		paragraphs[2].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["Chapter Two", "3"]);
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

	[Fact]
	public void Apply_TableOfContents_WithExplicitTabLeaderTemplate_PreservesParagraphTabsAndUsesTabRuns()
	{
		using var stream = CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate();
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
		var firstEntry = paragraphs[1];

		firstEntry.Descendants<TabChar>().Should().ContainSingle();
		var paragraphProperties = firstEntry.ParagraphProperties;
		paragraphProperties.Should().NotBeNull();
		var tabs = paragraphProperties!.Tabs;
		tabs.Should().NotBeNull();
		var tabStop = tabs!.Elements<DocumentFormat.OpenXml.Wordprocessing.TabStop>().Should().ContainSingle().Subject;
		tabStop.Val?.Value.Should().Be(TabStopValues.Right);
		tabStop.Leader?.Value.Should().Be(TabStopLeaderCharValues.Dot);

		var textNodes = firstEntry.Descendants<Text>().Select(text => text.Text).ToArray();
		textNodes.Should().HaveCount(2);
		textNodes[0].Should().Be("Chapter One");
		textNodes[1].Should().Be("2");
	}

	[Fact]
	public void Apply_TableOfContents_WithTemplateRunFormatting_PreservesRunProperties()
	{
		using var stream = CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		var firstEntry = doc.DocumentBody.Elements<Paragraph>().ElementAt(1);
		var runs = firstEntry.Descendants<Run>().ToArray();

		runs.Should().HaveCount(3);
		var firstRunProperties = runs[0].RunProperties;
		firstRunProperties.Should().NotBeNull();
		firstRunProperties!.Bold.Should().NotBeNull();
		firstRunProperties.FontSize?.Val?.Value.Should().Be("28");

		runs[1].Elements<TabChar>().Should().ContainSingle();
		var tabRunProperties = runs[1].RunProperties;
		tabRunProperties.Should().NotBeNull();
		tabRunProperties!.Italic.Should().NotBeNull();
		tabRunProperties.FontSize?.Val?.Value.Should().Be("24");

		var pageRunProperties = runs[2].RunProperties;
		pageRunProperties.Should().NotBeNull();
		pageRunProperties!.Italic.Should().NotBeNull();
		pageRunProperties.FontSize?.Val?.Value.Should().Be("20");
	}

	[Fact]
	public void Apply_TableOfFigures_FromCaptionParagraphs_RebuildsEntryParagraphs()
	{
		using var stream = CreateDocxWithTableOfFiguresField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(5);
		GetParagraphStyleId(paragraphs[1]).Should().Be("TOC1");
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Figure 1. Overview", "2"]);
		GetParagraphStyleId(paragraphs[2]).Should().Be("TOC1");
		paragraphs[2].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["Figure 2. Details", "3"]);
		paragraphs.Select(GetParagraphResultText).Should().NotContain("Old Figure\t99");
	}

	[Fact]
	public void Apply_TableOfFigures_FromSeqFigureParagraphs_RebuildsEntryParagraphs()
	{
		using var stream = CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(5);
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["1. Overview", "2"]);
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["2. Details", "3"]);
	}

	[Fact]
	public void Apply_SequenceFields_AssignsSequentialNumbersPerIdentifier()
	{
		using var stream = CreateDocxWithSequenceFields();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1 caption text");
		GetParagraphResultText(paragraphs[1]).Should().Be("2 more text");
		GetParagraphResultText(paragraphs[2]).Should().Be("1 first table");
	}

	[Fact]
	public void Apply_SequenceFields_WithResetSwitch_ResetsCounterToSpecifiedValue()
	{
		using var stream = CreateDocxWithSequenceFieldsAndResetSwitch();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1 first");
		GetParagraphResultText(paragraphs[1]).Should().Be("2 second");
		GetParagraphResultText(paragraphs[2]).Should().Be("10 reset");
		GetParagraphResultText(paragraphs[3]).Should().Be("11 after reset");
	}

	[Fact]
	public void Apply_SequenceFields_WithHiddenSwitch_IncrementsButHidesResult()
	{
		using var stream = CreateDocxWithSequenceFieldsAndHiddenSwitch();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1 visible");
		GetParagraphResultText(paragraphs[1]).Should().Be(" hidden counter");
		GetParagraphResultText(paragraphs[2]).Should().Be("3 skipped two");
	}

	[Fact]
	public void Apply_SequenceFields_WhenDisabled_DoesNotUpdateSeqValues()
	{
		using var stream = CreateDocxWithSequenceFields();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions { UpdateSequenceFields = false }
		});

		result.UpdatedFields.Should().NotContain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("X caption text");
	}

	[Fact]
	public void Apply_PageRefField_ResolvesToBookmarkPageNumber()
	{
		using var stream = CreateDocxWithPageRefField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("PAGEREF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("2");
	}

	[Fact]
	public void Apply_RefField_ResolvesToBookmarkText()
	{
		using var stream = CreateDocxWithRefField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

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