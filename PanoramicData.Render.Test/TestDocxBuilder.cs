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
}
