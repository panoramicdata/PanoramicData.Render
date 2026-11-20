namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Drawing;

internal static partial class TestDocxBuilder
{
	public static MemoryStream CreateDocxWithSingleEndnote(int endnoteId = 1, string text = "Endnote text")
	{
		return BuildWithBodyText(mainPart =>
		{
			var endnotesPart = mainPart.AddNewPart<EndnotesPart>();
			var separator = new Endnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
			separator.Append(new Paragraph(new Run(new SeparatorMark())));

			var note = new Endnote { Id = endnoteId };
			note.Append(new Paragraph(new Run(new Text(text))));

			endnotesPart.Endnotes = new Endnotes(separator, note);
		});
	}

	public static MemoryStream CreateDocxWithFootnotesAndEndnotes()
	{
		return BuildWithBodyText(mainPart =>
		{
			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var footnote = new Footnote { Id = 5 };
			footnote.Append(new Paragraph(new Run(new Text("Footnote A"))));
			footnotesPart.Footnotes = new Footnotes(footnote);

			var endnotesPart = mainPart.AddNewPart<EndnotesPart>();
			var endnote = new Endnote { Id = 7 };
			endnote.Append(new Paragraph(new Run(new Text("Endnote B"))));
			endnotesPart.Endnotes = new Endnotes(endnote);
		});
	}

	public static MemoryStream CreateDocxWithFootnotesPartWithoutRoot()
	{
		return BuildWithBodyText(mainPart =>
		{
			mainPart.AddNewPart<FootnotesPart>();
		});
	}

	public static MemoryStream CreateDocxWithEndnotesPartWithoutRoot()
	{
		return BuildWithBodyText(mainPart =>
		{
			mainPart.AddNewPart<EndnotesPart>();
		});
	}

	public static MemoryStream CreateDocxWithStylesPartWithoutStyles()
	{
		return BuildWithBodyText(mainPart =>
		{
			mainPart.AddNewPart<StyleDefinitionsPart>();
		});
	}

	public static MemoryStream CreateDocxWithDocDefaults(
		ParagraphPropertiesBaseStyle? paragraphDefaults,
		RunPropertiesBaseStyle? runDefaults)
	{
		return BuildWithBodyText(mainPart =>
		{
			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();

			var docDefaults = new DocDefaults();
			if (paragraphDefaults is not null)
			{
				docDefaults.Append(new ParagraphPropertiesDefault(paragraphDefaults));
			}

			if (runDefaults is not null)
			{
				docDefaults.Append(new RunPropertiesDefault(runDefaults));
			}

			stylesPart.Styles = new Styles(docDefaults);
		});
	}

	public static MemoryStream CreateDocxWithStylesWithoutDocDefaults()
	{
		return BuildWithBodyText(mainPart =>
		{
			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles();
		});
	}

	public static MemoryStream CreateDocxWithStyles(Styles styles)
	{
		return BuildWithBodyText(mainPart =>
		{
			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = styles;
		});
	}

	public static MemoryStream CreateDocxWithTheme(Drawing.Theme theme)
	{
		return BuildWithBodyText(mainPart =>
		{
			var themePart = mainPart.AddNewPart<ThemePart>();
			themePart.Theme = theme;
		});
	}

	public static MemoryStream CreateDocxWithThemeFontsAndColors()
	{
		var colorScheme = new Drawing.ColorScheme { Name = "CustomColors" };
		colorScheme.Append(
			new Drawing.Dark1Color(new Drawing.RgbColorModelHex { Val = "111111" }),
			new Drawing.Light1Color(new Drawing.RgbColorModelHex { Val = "EEEEEE" }),
			new Drawing.Dark2Color(new Drawing.SystemColor { Val = Drawing.SystemColorValues.WindowText, LastColor = "1F1F1F" }),
			new Drawing.Light2Color(new Drawing.RgbColorModelHex { Val = "FAFAFA" }),
			new Drawing.Accent1Color(new Drawing.RgbColorModelHex { Val = "4472C4" }),
			new Drawing.Accent2Color(new Drawing.RgbColorModelHex { Val = "ED7D31" }),
			new Drawing.Accent3Color(new Drawing.RgbColorModelHex { Val = "A5A5A5" }),
			new Drawing.Accent4Color(new Drawing.RgbColorModelHex { Val = "FFC000" }),
			new Drawing.Accent5Color(new Drawing.RgbColorModelHex { Val = "5B9BD5" }),
			new Drawing.Accent6Color(new Drawing.RgbColorModelHex { Val = "70AD47" }),
			new Drawing.Hyperlink(new Drawing.RgbColorModelHex { Val = "0563C1" }),
			new Drawing.FollowedHyperlinkColor(new Drawing.RgbColorModelHex { Val = "954F72" }));

		var majorFont = new Drawing.MajorFont(
			new Drawing.LatinFont { Typeface = "Aptos Display" },
			new Drawing.EastAsianFont { Typeface = "Yu Mincho" },
			new Drawing.ComplexScriptFont { Typeface = "Times New Roman" },
			new Drawing.SupplementalFont { Script = "Jpan", Typeface = "Yu Gothic" });

		var minorFont = new Drawing.MinorFont(
			new Drawing.LatinFont { Typeface = "Aptos" },
			new Drawing.EastAsianFont { Typeface = "Yu Gothic UI" },
			new Drawing.ComplexScriptFont { Typeface = "Arial" },
			new Drawing.SupplementalFont { Script = "Hans", Typeface = "Microsoft YaHei" });

		var fontScheme = new Drawing.FontScheme
		{
			Name = "CustomFonts"
		};
		fontScheme.Append(majorFont, minorFont);

		var formatScheme = new Drawing.FormatScheme { Name = "CustomFormat" };
		formatScheme.Append(
			new Drawing.FillStyleList(),
			new Drawing.LineStyleList(),
			new Drawing.EffectStyleList(),
			new Drawing.BackgroundFillStyleList());

		var themeElements = new Drawing.ThemeElements();
		themeElements.Append(colorScheme, fontScheme, formatScheme);

		var theme = new Drawing.Theme { Name = "CustomTheme" };
		theme.Append(themeElements);

		return CreateDocxWithTheme(theme);
	}

	public static MemoryStream CreateDocxWithThemePartWithoutThemeRoot()
	{
		return BuildWithBodyText(mainPart =>
		{
			mainPart.AddNewPart<ThemePart>();
		});
	}

	public static MemoryStream CreateDocxWithNumbering(Numbering numbering)
	{
		return BuildWithBodyText(mainPart =>
		{
			var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
			numberingPart.Numbering = numbering;
		});
	}

	public static MemoryStream CreateDocxWithNumberingPartWithoutRoot()
	{
		return BuildWithBodyText(mainPart =>
		{
			mainPart.AddNewPart<NumberingDefinitionsPart>();
		});
	}

	public static MemoryStream CreateDocxWithPageBreakBefore()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var pPr = new ParagraphProperties(new PageBreakBefore());
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("First paragraph"))),
				new Paragraph(pPr, new Run(new Text("Starts on new page")))));
		});
	}

	public static MemoryStream CreateDocxWithPageBreakBeforeDisabled()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var pPr = new ParagraphProperties(
				new PageBreakBefore { Val = new OnOffValue(false) });
			mainPart.Document = new Document(new Body(
				new Paragraph(pPr, new Run(new Text("No break")))));
		});
	}

	public static MemoryStream CreateDocxWithBookmarks()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				// Paragraph 1: bookmark that starts and ends within the same paragraph
				new Paragraph(
					new BookmarkStart { Id = "0", Name = "Introduction" },
					new Run(new Text("Introduction text")),
					new BookmarkEnd { Id = "0" }),
				// Paragraph 2: bookmark that starts here (ends in next paragraph)
				new Paragraph(
					new BookmarkStart { Id = "1", Name = "Chapter1" },
					new Run(new Text("Chapter 1 text"))),
				// Paragraph 3: bookmark from paragraph 2 ends here
				new Paragraph(
					new Run(new Text("More chapter 1 text")),
					new BookmarkEnd { Id = "1" })));
		});
	}

	public static MemoryStream CreateDocxWithBookmarkNoName()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(
					new BookmarkStart { Id = "0" },
					new Run(new Text("No name bookmark")),
					new BookmarkEnd { Id = "0" })));
		});
	}

	public static MemoryStream CreateDocxWithExternalHyperlink(string url = "https://example.com")
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var rel = mainPart.AddHyperlinkRelationship(new Uri(url), true);
			mainPart.Document = new Document(new Body(
				new Paragraph(
					new Hyperlink(
						new Run(new Text("Click here")))
					{ Id = rel.Id })));
		});
	}

	public static MemoryStream CreateDocxWithInternalBookmarkHyperlink(string bookmarkName = "MyBookmark")
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				// Paragraph 1: bookmark target
				new Paragraph(
					new BookmarkStart { Id = "0", Name = bookmarkName },
					new Run(new Text("Target paragraph")),
					new BookmarkEnd { Id = "0" }),
				// Paragraph 2: hyperlink to bookmark
				new Paragraph(
					new Hyperlink(
						new Run(new Text("Go to bookmark")))
					{ Anchor = bookmarkName })));
		});
	}

	public static MemoryStream CreateDocxWithMixedHyperlinks()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var rel = mainPart.AddHyperlinkRelationship(new Uri("https://example.com"), true);
			mainPart.Document = new Document(new Body(
				new Paragraph(
					new Run(new Text("Before ")),
					new Hyperlink(
						new Run(new Text("external link")))
					{ Id = rel.Id },
					new Run(new Text(" middle ")),
					new Hyperlink(
						new Run(new Text("internal link")))
					{ Anchor = "Section1" },
					new Run(new Text(" after")))));
		});
	}

	/// <summary>
	/// Creates a two-page DOCX with <c>EvenAndOddHeaders</c> enabled in document settings.
	/// Page 1 (odd) will get the default header "Odd Header".
	/// Page 2 (even) will get the even header "Even Header".
	/// </summary>
	public static MemoryStream CreateDocxWithEvenOddHeaders()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var oddHeaderPart = mainPart.AddNewPart<HeaderPart>();
			var oddRelId = mainPart.GetIdOfPart(oddHeaderPart);
			oddHeaderPart.Header = new Header(
				new Paragraph(new Run(new Text("Odd Header"))));

			var evenHeaderPart = mainPart.AddNewPart<HeaderPart>();
			var evenRelId = mainPart.GetIdOfPart(evenHeaderPart);
			evenHeaderPart.Header = new Header(
				new Paragraph(new Run(new Text("Even Header"))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = oddRelId },
				new HeaderReference { Type = HeaderFooterValues.Even, Id = evenRelId },
				new PageSize { Width = 12240, Height = 15840 },
				new PageMargin
				{
					Top = 1440, Bottom = 1440, Left = 1440, Right = 1440,
					Header = 720, Footer = 720
				});

			// Two pages of body text: enough paragraphs to force page 2
			var body = new Body();
			for (var i = 0; i < 60; i++)
			{
				body.AppendChild(new Paragraph(new Run(new Text($"Body paragraph {i + 1}"))));
			}

			body.AppendChild(sectPr);
			mainPart.Document = new Document(body);

			var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
			settingsPart.Settings = new Settings(new EvenAndOddHeaders());
		});
	}
	/// <summary>
	/// Creates a DOCX with two sections:
	/// <list type="bullet">
	/// <item>Section 1 (cover page): a <c>Default</c> header part and <c>titlePg</c> set, so the cover page itself shows no header.</item>
	/// <item>Section 2 (body): no <c>headerReference</c> elements — inherits section 1’s header by OOXML “link to previous” semantics.</item>
	/// </list>
	/// Pages in section 2 should display the section 1 default header text.
	/// </summary>
	/// <param name="headerText">Text to use in the section 1 default header.</param>
	public static MemoryStream CreateDocxWithSectionBreakAndInheritedHeader(string headerText = "Inherited Header")
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			// Section 1 default header (the cover page section owns this).
			var headerPart = mainPart.AddNewPart<HeaderPart>();
			var headerRelId = mainPart.GetIdOfPart(headerPart);
			headerPart.Header = new Header(
				new Paragraph(new Run(new Text(headerText))));

			// Section 1 sectPr: Default header ref + titlePg (first page is the cover, no header shown).
			var section1SectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = headerRelId },
				new TitlePage(),
				new PageSize { Width = 12240, Height = 15840 },
				new PageMargin
				{
					Top = 1440, Bottom = 1440, Left = 1440, Right = 1440,
					Header = 720, Footer = 720
				},
				new SectionType { Val = SectionMarkValues.NextPage });

			// Cover page paragraph that also carries the section 1 break.
			var coverPara = new Paragraph(
				new ParagraphProperties(section1SectPr),
				new Run(new Text("Cover page content")));

			// Section 2 body: no headerReference elements → inherits section 1’s headers.
			var bodySectPr = new SectionProperties(
				new PageSize { Width = 12240, Height = 15840 },
				new PageMargin
				{
					Top = 1440, Bottom = 1440, Left = 1440, Right = 1440,
					Header = 720, Footer = 720
				});

			var body = new Body();
			body.AppendChild(coverPara);
			// Enough paragraphs in section 2 to produce at least two pages.
			for (var i = 0; i < 60; i++)
			{
				body.AppendChild(new Paragraph(new Run(new Text($"Body paragraph {i + 1}"))));
			}

			body.AppendChild(bodySectPr);
			mainPart.Document = new Document(body);
		});
	}
}
