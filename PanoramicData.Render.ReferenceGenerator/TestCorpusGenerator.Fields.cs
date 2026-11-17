namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using V = DocumentFormat.OpenXml.Vml;

internal static partial class TestCorpusGenerator
{
	private static void GenerateRtlText(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		mainPart.Document = new Document(new Body(
			new Paragraph(
				new ParagraphProperties(new BiDi()),
				new Run(
					new RunProperties(new RightToLeftText()),
					new Text("مرحبا بالعالم هذا اختبار نص عربي من اليمين إلى اليسار"))),
			new Paragraph(
				new Run(new Text("Mixed LTR/RTL: English then العربية ثم English again."))),
			DefaultSectionProperties()));
	}

	private static void GenerateHeadersAndFooters(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Create header
		var headerPart = mainPart.AddNewPart<HeaderPart>();
		var headerRelId = mainPart.GetIdOfPart(headerPart);
		headerPart.Header = new Header(
			new Paragraph(
				new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
				new Run(
					new RunProperties(new Bold(), new FontSize { Val = "20" }),
					new Text("Document Header — Visual Regression Test"))));

		// Create footer
		var footerPart = mainPart.AddNewPart<FooterPart>();
		var footerRelId = mainPart.GetIdOfPart(footerPart);
		footerPart.Footer = new Footer(
			new Paragraph(
				new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
				new Run(
					new RunProperties(new FontSize { Val = "16" }),
					new Text("Page Footer — PanoramicData.Render"))));

		var sectPr = new SectionProperties(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440, Header = 720, Footer = 720 },
			new HeaderReference { Type = HeaderFooterValues.Default, Id = headerRelId },
			new FooterReference { Type = HeaderFooterValues.Default, Id = footerRelId });

		var body = new Body(
			new Paragraph(new Run(new Text(
				"This document has a header and a footer. The header is centered with bold text. The footer is right-aligned with smaller text."))),
			new Paragraph(new Run(new Text(
				"This second paragraph provides additional body content to verify that the header and footer appear correctly in relation to the body text."))),
			sectPr);

		mainPart.Document = new Document(body);
	}

	private static void GenerateMultiSection(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Section 1: US Letter portrait
		var sect1 = new SectionProperties(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 },
			new SectionType { Val = SectionMarkValues.NextPage });

		// Section 2 (final): US Letter landscape
		var sect2 = new SectionProperties(
			new PageSize { Width = 15840, Height = 12240, Orient = PageOrientationValues.Landscape },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 });

		var body = new Body(
			new Paragraph(new Run(new Text("Section 1: US Letter Portrait"))),
			new Paragraph(new Run(new Text("This is the first section of the document, using standard US Letter portrait orientation."))),
			new Paragraph(new ParagraphProperties((SectionProperties)sect1.CloneNode(true))),
			new Paragraph(new Run(new Text("Section 2: US Letter Landscape"))),
			new Paragraph(new Run(new Text("This is the second section, using landscape orientation. The page dimensions are swapped."))),
			sect2);

		mainPart.Document = new Document(body);
	}

	private static void GenerateTabStops(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Left tab at 2 inches, center tab at center, right tab at right margin
		var tabs = new Tabs(
			new TabStop { Val = TabStopValues.Left, Position = 2880 },
			new TabStop { Val = TabStopValues.Center, Position = 4680 },
			new TabStop { Val = TabStopValues.Right, Position = 9360 });

		var body = new Body(
			new Paragraph(new Run(new Text("Tab Stops Demo:"))),
			new Paragraph(
				new ParagraphProperties((Tabs)tabs.CloneNode(true)),
				new Run(
					new Text("Left") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("At 2in") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("Center") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("Right"))),
			new Paragraph(
				new ParagraphProperties(
					new Tabs(
						new TabStop { Val = TabStopValues.Right, Leader = TabStopLeaderCharValues.Dot, Position = 9360 })),
				new Run(
					new Text("Chapter 1") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("1"))),
			new Paragraph(
				new ParagraphProperties(
					new Tabs(
						new TabStop { Val = TabStopValues.Right, Leader = TabStopLeaderCharValues.Dot, Position = 9360 })),
				new Run(
					new Text("Chapter 2") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("15"))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GeneratePageBreak(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var body = new Body(
			new Paragraph(new Run(new Text("Page 1: This is the content on the first page."))),
			new Paragraph(new Run(new Break { Type = BreakValues.Page })),
			new Paragraph(new Run(new Text("Page 2: This content appears after an explicit page break."))),
			new Paragraph(new Run(new Break { Type = BreakValues.Page })),
			new Paragraph(new Run(new Text("Page 3: This is the third and final page."))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateWithToc(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Add Heading1/Heading2/Heading3 style definitions
		var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
		stylesPart.Styles = new Styles(
			MakeHeadingStyle("Heading1", 1),
			MakeHeadingStyle("Heading2", 2),
			MakeHeadingStyle("Heading3", 3));

		// TOC field with stale cached result
		var tocFieldParagraph = MakeComplexField("TOC \\o \"1-3\"", "Stale TOC entry\t99");

		// Stale TOC result paragraph (will be replaced during field update)
		var staleTocResult = new Paragraph(
			new ParagraphProperties(new ParagraphStyleId { Val = "TOC1" }),
			new Run(new Text("Stale entry") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("99")));

		var bodyChildren = new List<OpenXmlElement>
		{
			new Paragraph(new Run(new Text("Table of Contents Test Document"))),
			tocFieldParagraph,
			staleTocResult,
		};

		// Add three heading levels across multiple pages
		bodyChildren.Add(MakeStyledParagraph("Heading1", "Chapter 1: Introduction", pageBreak: true));
		bodyChildren.Add(new Paragraph(new Run(new Text("Introduction body text with enough content to occupy space on the page."))));
		bodyChildren.Add(MakeStyledParagraph("Heading2", "1.1 Background"));
		bodyChildren.Add(new Paragraph(new Run(new Text("Background section content providing additional detail."))));
		bodyChildren.Add(MakeStyledParagraph("Heading3", "1.1.1 Historical Context"));
		bodyChildren.Add(new Paragraph(new Run(new Text("Historical context text."))));

		bodyChildren.Add(MakeStyledParagraph("Heading1", "Chapter 2: Methods", pageBreak: true));
		bodyChildren.Add(new Paragraph(new Run(new Text("Methods section body text."))));
		bodyChildren.Add(MakeStyledParagraph("Heading2", "2.1 Approach"));
		bodyChildren.Add(new Paragraph(new Run(new Text("Approach description."))));

		bodyChildren.Add(MakeStyledParagraph("Heading1", "Chapter 3: Results", pageBreak: true));
		bodyChildren.Add(new Paragraph(new Run(new Text("Results section body text with findings and analysis."))));

		bodyChildren.Add(DefaultSectionProperties());

		mainPart.Document = new Document(new Body(bodyChildren));
	}

	private static void GenerateWithTof(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Add Caption style
		var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
		stylesPart.Styles = new Styles(
			new Style(new StyleName { Val = "Caption" })
			{
				Type = StyleValues.Paragraph,
				StyleId = "Caption"
			});

		// TOF field (TOC \f "Figure") with stale cached result
		var tofFieldParagraph = MakeComplexField("TOC \\f \"Figure\"", "Stale Figure entry\t99");

		// Stale TOF result paragraph
		var staleTofResult = new Paragraph(
			new ParagraphProperties(new ParagraphStyleId { Val = "TOC1" }),
			new Run(new Text("Stale figure entry") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("99")));

		var body = new Body(
			new Paragraph(new Run(new Text("Table of Figures Test Document"))),
			tofFieldParagraph,
			staleTofResult,
			MakeStyledParagraph("Caption", "Figure 1. System Architecture Diagram", pageBreak: true),
			new Paragraph(new Run(new Text("Description of the architecture diagram."))),
			MakeStyledParagraph("Caption", "Figure 2. Data Flow Overview", pageBreak: true),
			new Paragraph(new Run(new Text("Description of the data flow diagram."))),
			MakeStyledParagraph("Caption", "Figure 3. Results Chart", pageBreak: true),
			new Paragraph(new Run(new Text("Description of the results chart."))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateWithCrossRefs(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Page 1: PAGEREF and REF fields referencing a bookmark on page 2
		var pageRefField = MakeComplexField("PAGEREF _RefTarget", "??");
		var refField = MakeComplexField("REF _RefBookmark", "old text");

		// Page 2: bookmarked content
		var bookmarkedParagraph = new Paragraph(
			new ParagraphProperties(new PageBreakBefore()),
			new BookmarkStart { Id = "1", Name = "_RefTarget" },
			new BookmarkStart { Id = "2", Name = "_RefBookmark" },
			new Run(new Text("Target Heading Content")),
			new BookmarkEnd { Id = "1" },
			new BookmarkEnd { Id = "2" });

		var body = new Body(
			new Paragraph(new Run(new Text("Cross-Reference Test Document"))),
			new Paragraph(
				new Run(new Text("See page ") { Space = SpaceProcessingModeValues.Preserve })),
			pageRefField,
			new Paragraph(
				new Run(new Text("Reference text: ") { Space = SpaceProcessingModeValues.Preserve })),
			refField,
			bookmarkedParagraph,
			new Paragraph(new Run(new Text("Additional content on the second page provides more material for layout."))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	// --- Helper methods ---
}
