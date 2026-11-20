namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

internal static class SimpleDocxBuilder
{
	internal static MemoryStream Create(int paragraphCount, Func<int, string> textForParagraph)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (var i = 0; i < paragraphCount; i++)
			{
				body.AppendChild(new Paragraph(new Run(new Text(textForParagraph(i)))));
			}

			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}
}
