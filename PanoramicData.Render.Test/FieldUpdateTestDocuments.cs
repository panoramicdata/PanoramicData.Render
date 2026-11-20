namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

internal static partial class FieldUpdateTestDocuments
{
	internal static MemoryStream CreateDocxWithPageFieldParagraphs()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("PAGE", "999"),
			CreateComplexFieldParagraph("PAGE", "999", pageBreakBefore: true),
			CreateComplexFieldParagraph("NUMPAGES", "999"));
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
		return BuildDocx(
			CreateComplexFieldParagraph(instruction, string.Empty),
			CreateStyledParagraph("TOC1", "Old Entry\t99"),
			CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
			CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true));
	}

	internal static MemoryStream CreateDocxWithTableOfFiguresField()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("TOC \\f \"Figure\"", string.Empty),
			CreateStyledParagraph("TOC1", "Old Figure\t99"),
			CreateStyledParagraph("Caption", "Figure 1. Overview", pageBreakBefore: true),
			CreateStyledParagraph("Caption", "Figure 2. Details", pageBreakBefore: true));
	}

	internal static MemoryStream CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("TOC \\f \"Figure\"", string.Empty),
			CreateStyledParagraph("TOC1", "Old Figure\t99"),
			CreateSeqFigureParagraph("99", ". Overview", pageBreakBefore: true),
			CreateSeqFigureParagraph("99", ". Details", pageBreakBefore: true));
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

		return BuildDocx([.. bodyChildren]);
	}
	internal static MemoryStream CreateDocxWithCustomStyleTableOfContentsField()
	{
		var styles = new Styles(
			new Style(
				new StyleName { Val = "Custom Heading" },
				new PrimaryStyle())
			{
				Type = StyleValues.Paragraph,
				StyleId = "CustomHeading"
			});

		return BuildDocxWithStyles(
			styles,
			CreateComplexFieldParagraph("TOC \\t \"Custom Heading,2\"", string.Empty),
			CreateStyledParagraph("TOC1", "Old Entry\t99"),
			CreateStyledParagraph("CustomHeading", "Appendix A", pageBreakBefore: true));
	}

	internal static MemoryStream CreateDocxWithHyperlinkedTableOfContentsField()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("TOC \\o \"1-3\" \\h", string.Empty),
			CreateStyledParagraph("TOC1", "Old Entry\t99"),
			CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true, bookmarkName: "_TocChapterOne", bookmarkId: 1),
			CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true, bookmarkName: "_TocChapterTwo", bookmarkId: 2));
	}

	internal static MemoryStream CreateDocxWithHyperlinkedTableOfContentsFieldWithoutHeadingBookmarks()
	{
		return BuildDocx(
			CreateComplexFieldParagraph("TOC \\o \"1-3\" \\h", string.Empty),
			CreateStyledParagraph("TOC1", "Old Entry\t99"),
			CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
			CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true));
	}

	internal static MemoryStream CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate()
	{
		return CreateDocxWithTocTemplate(
			new Run(new Text("Old Entry") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("99") { Space = SpaceProcessingModeValues.Preserve }));
	}

	internal static MemoryStream CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting()
	{
		return CreateDocxWithTocTemplate(
			new Run(
				new RunProperties(new Bold(), new FontSize { Val = "28" }),
				new Text("Old Entry") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(
				new RunProperties(new Italic(), new FontSize { Val = "24" }),
				new TabChar()),
			new Run(
				new RunProperties(new Italic(), new FontSize { Val = "20" }),
				new Text("99") { Space = SpaceProcessingModeValues.Preserve }));
	}

	internal static MemoryStream CreateDocxWithTableOfContentsFieldAndStyleDefinedRunFormatting()
	{
		var styles = new Styles(
			new Style(
				new StyleName { Val = "TOC 1" },
				new StyleParagraphProperties(CreateDotLeaderTabs()),
				new StyleRunProperties(new Bold(), new FontSize { Val = "28" }))
			{
				Type = StyleValues.Paragraph,
				StyleId = "TOC1"
			});

		return BuildDocxWithStyles(
			styles,
			CreateComplexFieldParagraph("TOC \\o \"1-3\"", string.Empty),
			CreateStyledParagraph("TOC1", "Old Entry\t99"),
			CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
			CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true));
	}

	internal static Tabs CreateDotLeaderTabs()
		=> new(new DocumentFormat.OpenXml.Wordprocessing.TabStop
		{
			Val = TabStopValues.Right,
			Position = 9360,
			Leader = TabStopLeaderCharValues.Dot
		});

	internal static MemoryStream CreateDocxWithTocTemplate(params OpenXmlElement[] templateRuns)
	{
		var template = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "TOC1" }, CreateDotLeaderTabs()));
		template.Append(templateRuns);
		return BuildDocx(
			CreateComplexFieldParagraph("TOC \\o \"1-3\"", string.Empty),
			template,
			CreateStyledParagraph("Heading1", "Chapter One", pageBreakBefore: true),
			CreateStyledParagraph("Heading1", "Chapter Two", pageBreakBefore: true));
	}
	internal static MemoryStream BuildDocx(params OpenXmlElement[] body) => BuildDocxWithStyles(null, body);

	internal static MemoryStream BuildDocxWithStyles(Styles? styles, params OpenXmlElement[] body)
	{
		var stream = new MemoryStream();
		using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = document.AddMainDocumentPart();
			if (styles is not null)
			{
				mainPart.AddNewPart<StyleDefinitionsPart>().Styles = styles;
			}

			mainPart.Document = new Document(new Body(body));
		}

		stream.Position = 0;
		return stream;
	}
	internal static Paragraph CreateComplexFieldParagraph(string instruction, string cachedValue, bool pageBreakBefore = false)
		=> CreateFieldParagraph(instruction, cachedValue, null, pageBreakBefore);
}
