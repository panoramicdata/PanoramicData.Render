namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static void MergeProperties(OpenXmlCompositeElement target, OpenXmlCompositeElement? source)
	{
		if (source is null)
		{
			return;
		}

		foreach (var child in source.ChildElements)
		{
			var existing = target.ChildElements.FirstOrDefault(element => element.LocalName == child.LocalName && element.NamespaceUri == child.NamespaceUri);
			existing?.Remove();
			target.Append(child.CloneNode(true));
		}
	}

	private static Run CreateRun(OpenXmlElement child, RunProperties? runProperties)
	{
		var run = new Run();
		if (runProperties is not null)
		{
			run.AppendChild((RunProperties)runProperties.CloneNode(true));
		}

		run.AppendChild(child);
		return run;
	}

	private static string GetParagraphText(Paragraph paragraph)
		=> string.Concat(paragraph.Descendants<Text>().Select(text => text.Text));

	private static string NormalizeWhitespace(string value)
		=> string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
