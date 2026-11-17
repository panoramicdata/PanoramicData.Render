namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static Dictionary<string, int> BuildBookmarkPageMap(
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap)
	{
		var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var paragraphBlock in blocks.OfType<ParagraphBlock>())
		{
			if (!blockPageMap.TryGetValue(paragraphBlock, out var pageNumber))
			{
				continue;
			}

			foreach (var bookmarkStart in paragraphBlock.SourceElement.Elements<BookmarkStart>())
			{
				var name = bookmarkStart.Name?.Value;
				if (!string.IsNullOrWhiteSpace(name))
				{
					map.TryAdd(name, pageNumber);
				}
			}
		}

		return map;
	}

	private static Dictionary<string, string> BuildBookmarkTextMap(Body documentBody)
	{
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var paragraph in documentBody.Descendants<Paragraph>())
		{
			foreach (var bookmarkStart in paragraph.Elements<BookmarkStart>())
			{
				var name = bookmarkStart.Name?.Value;
				var id = bookmarkStart.Id?.Value;
				if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(id))
				{
					map.TryAdd(name, CollectBookmarkText(bookmarkStart, id));
				}
			}
		}

		return map;
	}

	private static string CollectBookmarkText(BookmarkStart bookmarkStart, string id)
	{
		var textBuilder = new StringBuilder();
		var sibling = bookmarkStart.NextSibling();
		while (sibling is not null && !IsBookmarkEnd(sibling, id))
		{
			if (sibling is Run run)
			{
				foreach (var text in run.Descendants<Text>())
				{
					textBuilder.Append(text.Text);
				}
			}

			sibling = sibling.NextSibling();
		}

		return textBuilder.ToString();
	}

	private static bool IsBookmarkEnd(OpenXmlElement element, string id)
		=> element is BookmarkEnd end && string.Equals(end.Id?.Value, id, StringComparison.Ordinal);
	private static bool UpdateCrossReferenceFields(
		Body documentBody,
		FieldUpdateOptions options,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
	{
		if (!options.UpdateCrossReferences)
		{
			return false;
		}

		var hasChanges = false;

		foreach (var paragraph in documentBody.Descendants<Paragraph>())
		{
			hasChanges |= UpdateCrossReferenceComplexFields(paragraph, bookmarkPageMap, bookmarkTextMap, updatedFields);
			hasChanges |= UpdateCrossReferenceSimpleFields(paragraph, bookmarkPageMap, bookmarkTextMap, updatedFields);
		}

		return hasChanges;
	}
}
