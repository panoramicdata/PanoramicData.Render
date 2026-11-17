namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Xunit;

public sealed partial class DocxRendererTests
{
	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndExplicitTabLeaderTemplate_EmitsLeaderDotsInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().Contain(">2<");
		firstPageSvg.Should().Contain(">.<");
		firstPageSvg.Should().NotContain("Old Entry");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndTemplateRunFormatting_PreservesBoldAndItalicInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().Contain("font-weight=\"bold\"");
		firstPageSvg.Should().Contain("font-style=\"italic\"");
		firstPageSvg.Should().Contain("font-size=\"14pt\"");
		firstPageSvg.Should().Contain("font-size=\"10pt\"");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndStyleDefinedRunFormatting_UsesTocParagraphStyleFormattingInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldAndStyleDefinedRunFormatting();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().Contain("font-weight=\"bold\"");
		firstPageSvg.Should().Contain("font-size=\"14pt\"");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfFiguresFieldUpdate_RendersGeneratedEntriesOnFirstPage()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfFiguresField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Figure 1. Overview");
		firstPageSvg.Should().Contain(">2<");
		firstPageSvg.Should().Contain("Figure 2. Details");
		firstPageSvg.Should().Contain(">3<");
		firstPageSvg.Should().NotContain("Old Figure");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOF");
	}

	[Fact]
	public void Render_WithTableOfFiguresFieldUpdateAndSeqFigureParagraphs_RendersGeneratedEntriesOnFirstPage()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("1. Overview");
		firstPageSvg.Should().Contain(">2<");
		firstPageSvg.Should().Contain("2. Details");
		firstPageSvg.Should().Contain(">3<");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOF");
	}

	[Fact]
	public void Render_WithPageRefFieldUpdate_RendersResolvedPageNumberInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithPageRefField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain(">2<");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("PAGEREF");
	}

	[Fact]
	public void Render_WithRefFieldUpdate_RendersResolvedBookmarkTextInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithRefField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Target Text");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("REF");
	}

	[Fact]
	public void RenderedPage_LayoutPage_ExposesInternalPage()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = renderer.Render(stream);
		var page = result.Pages[0];

		page.LayoutPage.Should().NotBeNull();
		page.LayoutPage.PageNumber.Should().Be(1);
	}

	[Fact]
	public void Render_EvenAndOddHeaders_Page2ReceivesEvenHeader()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateDocxWithEvenOddHeaders();

		var result = renderer.Render(stream);

		result.Pages.Should().HaveCountGreaterThanOrEqualTo(2, "document has enough content for two pages");

		var page1Svg = result.Pages[0].ToSvg();
		var page2Svg = result.Pages[1].ToSvg();

		page1Svg.Should().Contain("Odd Header", "page 1 is an odd page");
		page2Svg.Should().Contain("Even Header", "page 2 is an even page and EvenAndOddHeaders is set");
		page2Svg.Should().NotContain("Odd Header", "odd header must not appear on even pages");
	}

	[Fact]
	public void Render_Section2HasNoHeaderRefs_InheritsSection1Header()
	{
		// When section 2's sectPr has no headerReference elements (OOXML "link to previous"),
		// all pages in section 2 must display the Default header defined by section 1.
		// The titlePage flag on section 1 must NOT suppress the header on the first page
		// of section 2 (it only suppresses the first page of section 1 itself).
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateDocxWithSectionBreakAndInheritedHeader("Inherited Header");

		var result = renderer.Render(stream);

		result.Pages.Should().HaveCountGreaterThanOrEqualTo(2, "document has a cover page plus at least one body page");

		var page1Svg = result.Pages[0].ToSvg();
		var page2Svg = result.Pages[1].ToSvg();
		var page3Svg = result.Pages.Count > 2 ? result.Pages[2].ToSvg() : null;

		page1Svg.Should().NotContain("Inherited Header", "page 1 is the cover page; section 1 has titlePg set and no First header, so no header appears");
		page2Svg.Should().Contain("Inherited Header", "page 2 is the first page of section 2 which inherits section 1's Default header");
		page3Svg?.Should().Contain("Inherited Header", "page 3 is a subsequent body page and must also inherit the Default header");
	}

	private sealed class RecordingLogger : ILogger
	{
		public List<string> WarningMessages { get; } = [];

		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull
		{
			ArgumentNullException.ThrowIfNull(state);
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			_ = eventId;
			if (logLevel == LogLevel.Warning)
			{
				WarningMessages.Add(formatter(state, exception));
			}
		}
	}

	private static MemoryStream CreateDocxWithParagraphs(int count)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (var i = 0; i < count; i++)
			{
				body.AppendChild(new Paragraph(new Run(new Text($"Paragraph {i + 1}"))));
			}

			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}

	[Fact]
	public void Render_WithStyleDefinedHeadingNumbering_EmitsMultiLevelLabels()
	{
		// Heading numbering is defined in the paragraph style (not directly on the paragraph).
		// This simulates the real-world pattern used by Word when "Define new Multilevel list"
		// is linked to heading styles.
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = CreateDocxWithStyleDefinedHeadingNumbering();

		var result = renderer.Render(stream);
		var svg = result.Pages[0].ToSvg();

		// Heading 1 (level 0, %1.) should produce label "1. " (label + appended space)
		svg.Should().Contain(">1. <");
		// Heading 2 (level 1, %1.%2.) should produce label "1.1. " (label + appended space)
		svg.Should().Contain(">1.1. <");
	}

	private static MemoryStream CreateDocxWithStyleDefinedHeadingNumbering()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			// Numbering: two-level decimal list, patterns "%1." and "%1.%2."
			var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
			var abstractNum = new AbstractNum(
				new Level(
					new StartNumberingValue { Val = 1 },
					new NumberingFormat { Val = NumberFormatValues.Decimal },
					new LevelText { Val = "%1." }) { LevelIndex = 0 },
				new Level(
					new StartNumberingValue { Val = 1 },
					new NumberingFormat { Val = NumberFormatValues.Decimal },
					new LevelText { Val = "%1.%2." }) { LevelIndex = 1 })
			{ AbstractNumberId = 1 };
			var numInstance = new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 };
			numberingPart.Numbering = new Numbering(abstractNum, numInstance);

			// Styles: Heading1 (ilvl=0) and Heading2 (ilvl=1) with numPr in the style's pPr
			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles(
				new Style(
					new StyleName { Val = "heading 1" },
					new StyleParagraphProperties(
						new NumberingProperties(
							new NumberingLevelReference { Val = 0 },
							new NumberingId { Val = 1 })))
				{
					Type = StyleValues.Paragraph,
					StyleId = "Heading1"
				},
				new Style(
					new StyleName { Val = "heading 2" },
					new StyleParagraphProperties(
						new NumberingProperties(
							new NumberingLevelReference { Val = 1 },
							new NumberingId { Val = 1 })))
				{
					Type = StyleValues.Paragraph,
					StyleId = "Heading2"
				});

			// Body paragraphs: pStyle only, no direct numPr
			mainPart.Document = new Document(new Body(
				new Paragraph(
					new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
					new Run(new Text("First Heading"))),
				new Paragraph(
					new ParagraphProperties(new ParagraphStyleId { Val = "Heading2" }),
					new Run(new Text("Sub-heading")))));
		}

		stream.Position = 0;
		return stream;
	}

	private static MemoryStream CreateDocxWithHeadingStyleFormatting()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var paragraph = new Paragraph(
				new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
				new Run(new Text("Styled heading text")));
			mainPart.Document = new Document(new Body(paragraph));

			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles(
				new Style(
					new StyleName { Val = "Heading 1" },
					new StyleRunProperties(
						new Bold(),
						new FontSize { Val = "56" },
						new Color { Val = "ED7D31" }))
				{
					Type = StyleValues.Paragraph,
					StyleId = "Heading1"
				});
		}

		stream.Position = 0;
		return stream;
	}
}
