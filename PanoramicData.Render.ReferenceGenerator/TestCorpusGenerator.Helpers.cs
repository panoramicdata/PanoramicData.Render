namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using V = DocumentFormat.OpenXml.Vml;

internal static partial class TestCorpusGenerator
{
	private static SectionProperties DefaultSectionProperties() =>
		new(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 });

	private static Paragraph MakeParagraph(string text, JustificationValues alignment) =>
		new(
			new ParagraphProperties(new Justification { Val = alignment }),
			new Run(new Text(text)));

	private static Paragraph MakeIndentedParagraph(string text,
		string? left = null, string? right = null,
		string? firstLine = null, string? hanging = null)
	{
		var ind = new Indentation();
		if (left is not null)
		{
			ind.Left = left;
		}
		if (right is not null)
		{
			ind.Right = right;
		}
		if (firstLine is not null)
		{
			ind.FirstLine = firstLine;
		}
		if (hanging is not null)
		{
			ind.Hanging = hanging;
		}

		return new Paragraph(
			new ParagraphProperties(ind),
			new Run(new Text(text)));
	}

	private static TableCell MakeTableCell(string text, bool bold = false)
	{
		var run = bold
			? new Run(new RunProperties(new Bold()), new Text(text))
			: new Run(new Text(text));

		return new TableCell(new Paragraph(run));
	}

	private static void AddTableStyleDefinitions(MainDocumentPart mainPart, params Style[] styles)
	{
		var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
		stylesPart.Styles = new Styles(styles);
	}

	private static Style CreateTableStyle(string styleId, params (TableStyleOverrideValues Type, Shading Shading)[] overrides)
	{
		var styleChildren = new List<OpenXmlElement>();
		foreach (var styleOverride in overrides)
		{
			styleChildren.Add(new TableStyleProperties(new TableCellProperties((Shading)styleOverride.Shading.CloneNode(true)))
			{
				Type = styleOverride.Type
			});
		}

		var style = new Style(styleChildren)
		{
			Type = StyleValues.Table,
			StyleId = styleId,
			CustomStyle = true
		};

		style.Append(new StyleName { Val = styleId });
		return style;
	}

	private static Shading CreateShading(string fillHex)
	{
		var shading = new Shading { Fill = fillHex };
		shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		return shading;
	}

	private static TableLook CreateTableLook(
		bool applyFirstRow = false,
		bool applyLastRow = false,
		bool applyFirstColumn = false,
		bool applyLastColumn = false,
		bool applyBandedRows = false,
		bool applyBandedColumns = false)
	{
		var look = new TableLook();
		SetLookFlag(look, "firstRow", applyFirstRow);
		SetLookFlag(look, "lastRow", applyLastRow);
		SetLookFlag(look, "firstColumn", applyFirstColumn);
		SetLookFlag(look, "lastColumn", applyLastColumn);
		SetLookFlag(look, "noHBand", !applyBandedRows);
		SetLookFlag(look, "noVBand", !applyBandedColumns);
		return look;
	}

	private static void SetLookFlag(TableLook look, string name, bool value)
		=> look.SetAttribute(new OpenXmlAttribute("w", name, "http://schemas.openxmlformats.org/wordprocessingml/2006/main", value ? "1" : "0"));

	private static Level MakeLevel(int levelIndex, string numFmt, string lvlText)
	{
		var level = new Level(
			new StartNumberingValue { Val = 1 },
			new NumberingFormat { Val = numFmt switch
			{
				"decimal" => NumberFormatValues.Decimal,
				"lowerLetter" => NumberFormatValues.LowerLetter,
				"lowerRoman" => NumberFormatValues.LowerRoman,
				_ => NumberFormatValues.Decimal
			} },
			new LevelText { Val = lvlText },
			new ParagraphProperties(
				new Indentation
				{
					Left = ((levelIndex + 1) * 720).ToString(),
					Hanging = "360"
				}))
		{ LevelIndex = levelIndex };

		return level;
	}

	private static Paragraph MakeListParagraph(string text, int numId, int ilvl) =>
		new(
			new ParagraphProperties(
				new NumberingProperties(
					new NumberingLevelReference { Val = ilvl },
					new NumberingId { Val = numId })),
			new Run(new Text(text)));

	private static Style MakeHeadingStyle(string styleId, int outlineLevel)
	{
		var style = new Style(
			new StyleName { Val = $"heading {outlineLevel}" },
			new StyleParagraphProperties(
				new OutlineLevel { Val = outlineLevel - 1 }))
		{
			Type = StyleValues.Paragraph,
			StyleId = styleId
		};

		return style;
	}

	private static Paragraph MakeStyledParagraph(string styleId, string text, bool pageBreak = false)
	{
		var properties = new ParagraphProperties(new ParagraphStyleId { Val = styleId });
		if (pageBreak)
		{
			properties.AppendChild(new PageBreakBefore());
		}

		return new Paragraph(properties, new Run(new Text(text)));
	}

	private static Paragraph MakeComplexField(string instruction, string cachedValue)
	{
		return new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = $" {instruction} " }),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedValue)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
	}

	private static void BuildDocument(string path, Func<MainDocumentPart, IEnumerable<OpenXmlElement>> buildBody)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();
		mainPart.Document = new Document(new Body(buildBody(mainPart)));
	}

	private static Paragraph TextParagraph(string text) => new(new Run(new Text(text)));

	private static IEnumerable<OpenXmlElement> TitledBody(string title, params OpenXmlElement[] content) =>
		[TextParagraph(title), .. content, DefaultSectionProperties()];

	private static TableRow MakeRow(bool bold, params string[] texts) =>
		new(texts.Select(text => MakeTableCell(text, bold)));

	private static TableBorders CreateSingleBorders() =>
		new(
			new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
			new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
			new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
			new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
			new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
			new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" });
}
