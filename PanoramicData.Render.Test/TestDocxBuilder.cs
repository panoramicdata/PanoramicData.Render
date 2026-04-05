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
}
