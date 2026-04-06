namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Drawing;

internal static class TestDocxBuilder
{
	public static MemoryStream CreateMinimalDocx()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Test")))));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateFullDocx()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Test")))));

			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles();

			var themePart = mainPart.AddNewPart<ThemePart>();
			themePart.Theme = new Drawing.Theme { Name = "Test" };

			var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
			numberingPart.Numbering = new Numbering();

			var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
			settingsPart.Settings = new Settings();
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithoutMainPart()
	{
		var stream = new MemoryStream();
		using (var _ = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			// Intentionally don't add MainDocumentPart
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithoutBody()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(); // No Body
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithSectionProperties(SectionProperties sectPr)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Test"))),
				sectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithMultipleSections()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			// First section: landscape A4 (section break in paragraph properties)
			var firstSectPr = new SectionProperties(
				new PageSize
				{
					Width = 16838,
					Height = 11906,
					Orient = PageOrientationValues.Landscape
				},
				new SectionType { Val = SectionMarkValues.NextPage });

			var para1 = new Paragraph(
				new ParagraphProperties(firstSectPr),
				new Run(new Text("Section 1")));

			// Final section: portrait US Letter (body-level section properties)
			var finalSectPr = new SectionProperties(
				new PageSize { Width = 12240, Height = 15840 });

			mainPart.Document = new Document(new Body(
				para1,
				new Paragraph(new Run(new Text("Section 2"))),
				finalSectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithStyledParagraph(string styleId)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var pPr = new ParagraphProperties(
				new ParagraphStyleId { Val = styleId });
			mainPart.Document = new Document(new Body(
				new Paragraph(pPr, new Run(new Text("Styled")))));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithTable()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var table = new Table(
				new TableRow(
					new TableCell(
						new Paragraph(new Run(new Text("Cell 1"))))));
			mainPart.Document = new Document(new Body(
				table,
				new Paragraph(new Run(new Text("After table")))));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithParagraphs(int count)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (int i = 0; i < count; i++)
			{
				body.Append(new Paragraph(new Run(new Text($"Paragraph {i + 1}"))));
			}

			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithMixedContent()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var table = new Table(
				new TableRow(
					new TableCell(
						new Paragraph(new Run(new Text("Cell"))))));
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Before"))),
				table,
				new Paragraph(new Run(new Text("After"))),
				new Paragraph(new Run(new Text("Last")))));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithEmptyBody()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body());
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithNumberedParagraph(int numId, int ilvl)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var numPr = new NumberingProperties(
				new NumberingLevelReference { Val = ilvl },
				new NumberingId { Val = numId });
			var pPr = new ParagraphProperties(numPr);
			mainPart.Document = new Document(new Body(
				new Paragraph(pPr, new Run(new Text("List item")))));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithDefaultHeader(string headerText = "Header Text")
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var headerPart = mainPart.AddNewPart<HeaderPart>();
			var relId = mainPart.GetIdOfPart(headerPart);
			headerPart.Header = new Header(
				new Paragraph(new Run(new Text(headerText))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithDefaultFooter(string footerText = "Footer Text")
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var footerPart = mainPart.AddNewPart<FooterPart>();
			var relId = mainPart.GetIdOfPart(footerPart);
			footerPart.Footer = new Footer(
				new Paragraph(new Run(new Text(footerText))));

			var sectPr = new SectionProperties(
				new FooterReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithMultipleHeaders()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var defaultHeaderPart = mainPart.AddNewPart<HeaderPart>();
			var defaultRelId = mainPart.GetIdOfPart(defaultHeaderPart);
			defaultHeaderPart.Header = new Header(
				new Paragraph(new Run(new Text("Default Header"))));

			var firstHeaderPart = mainPart.AddNewPart<HeaderPart>();
			var firstRelId = mainPart.GetIdOfPart(firstHeaderPart);
			firstHeaderPart.Header = new Header(
				new Paragraph(new Run(new Text("First Page Header"))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = defaultRelId },
				new HeaderReference { Type = HeaderFooterValues.First, Id = firstRelId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithHeaderContainingTable()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var headerPart = mainPart.AddNewPart<HeaderPart>();
			var relId = mainPart.GetIdOfPart(headerPart);
			headerPart.Header = new Header(
				new Paragraph(new Run(new Text("Header Text"))),
				new Table(
					new TableRow(
						new TableCell(
							new Paragraph(new Run(new Text("Header Cell")))))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithFooterContainingMixedContent()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();

			var footerPart = mainPart.AddNewPart<FooterPart>();
			var relId = mainPart.GetIdOfPart(footerPart);
			footerPart.Footer = new Footer(
				new Paragraph(new Run(new Text("Before table"))),
				new Table(
					new TableRow(
						new TableCell(
							new Paragraph(new Run(new Text("Footer Cell")))))),
				new Paragraph(new Run(new Text("After table"))));

			var sectPr = new SectionProperties(
				new FooterReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithSingleFootnote(int footnoteId = 1, string text = "Footnote text")
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var separator = new Footnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
			separator.Append(new Paragraph(new Run(new SeparatorMark())));

			var note = new Footnote { Id = footnoteId };
			note.Append(new Paragraph(new Run(new Text(text))));

			footnotesPart.Footnotes = new Footnotes(separator, note);
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithFootnoteContainingTable(int footnoteId = 2)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var note = new Footnote { Id = footnoteId };
			note.Append(
				new Paragraph(new Run(new Text("Before table"))),
				new Table(
					new TableRow(
						new TableCell(
							new Paragraph(new Run(new Text("Cell")))))),
				new Paragraph(new Run(new Text("After table"))));

			footnotesPart.Footnotes = new Footnotes(note);
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithSingleEndnote(int endnoteId = 1, string text = "Endnote text")
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var endnotesPart = mainPart.AddNewPart<EndnotesPart>();
			var separator = new Endnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
			separator.Append(new Paragraph(new Run(new SeparatorMark())));

			var note = new Endnote { Id = endnoteId };
			note.Append(new Paragraph(new Run(new Text(text))));

			endnotesPart.Endnotes = new Endnotes(separator, note);
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithFootnotesAndEndnotes()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var footnote = new Footnote { Id = 5 };
			footnote.Append(new Paragraph(new Run(new Text("Footnote A"))));
			footnotesPart.Footnotes = new Footnotes(footnote);

			var endnotesPart = mainPart.AddNewPart<EndnotesPart>();
			var endnote = new Endnote { Id = 7 };
			endnote.Append(new Paragraph(new Run(new Text("Endnote B"))));
			endnotesPart.Endnotes = new Endnotes(endnote);
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithFootnotesPartWithoutRoot()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			mainPart.AddNewPart<FootnotesPart>();
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithEndnotesPartWithoutRoot()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			mainPart.AddNewPart<EndnotesPart>();
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithStylesPartWithoutStyles()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			mainPart.AddNewPart<StyleDefinitionsPart>();
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithDocDefaults(
		ParagraphPropertiesBaseStyle? paragraphDefaults,
		RunPropertiesBaseStyle? runDefaults)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

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
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithStylesWithoutDocDefaults()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles();
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithStyles(Styles styles)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = styles;
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithTheme(Drawing.Theme theme)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var themePart = mainPart.AddNewPart<ThemePart>();
			themePart.Theme = theme;
		}

		stream.Position = 0;
		return stream;
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
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			mainPart.AddNewPart<ThemePart>();
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithNumbering(Numbering numbering)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
			numberingPart.Numbering = numbering;
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateDocxWithNumberingPartWithoutRoot()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			mainPart.AddNewPart<NumberingDefinitionsPart>();
		}

		stream.Position = 0;
		return stream;
	}
}
