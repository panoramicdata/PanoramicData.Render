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
				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				var textBuilder = new StringBuilder();
				var sibling = bookmarkStart.NextSibling();
				while (sibling is not null)
				{
					if (sibling is BookmarkEnd end && string.Equals(end.Id?.Value, id, StringComparison.Ordinal))
					{
						break;
					}

					if (sibling is Run run)
					{
						foreach (var text in run.Descendants<Text>())
						{
							textBuilder.Append(text.Text);
						}
					}

					sibling = sibling.NextSibling();
				}

				map.TryAdd(name, textBuilder.ToString());
			}
		}

		return map;
	}

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

	private static bool UpdateCrossReferenceComplexFields(
		Paragraph paragraph,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
		=> ProcessComplexFieldResults(
			paragraph,
			(text, activeField) => UpdateCrossReferenceResultText(text, activeField, bookmarkPageMap, bookmarkTextMap, updatedFields));
	private static bool UpdateCrossReferenceResultText(
		Text text,
		ActiveFieldUpdate activeField,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
	{
		if (!activeField.IsResultSection)
		{
			return false;
		}

		if (!TryComputeCrossReferenceValue(activeField.InstructionBuilder.ToString(), bookmarkPageMap, bookmarkTextMap, out var computedValue, out var fieldName))
		{
			return false;
		}

		return WriteComplexResult(text, activeField, computedValue, fieldName, updatedFields);
	}

	private static bool UpdateCrossReferenceSimpleFields(
		Paragraph paragraph,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
	{
		var hasChanges = false;

		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			if (TryComputeCrossReferenceValue(simpleField.Instruction?.Value, bookmarkPageMap, bookmarkTextMap, out var computedValue, out var fieldName))
			{
				hasChanges |= WriteSimpleResult(simpleField, computedValue, fieldName, true, updatedFields);
			}
		}

		return hasChanges;
	}
	private static bool TryComputeCrossReferenceValue(
		string? instruction,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		out string computedValue,
		out string fieldName)
	{
		computedValue = string.Empty;
		fieldName = string.Empty;

		if (string.IsNullOrWhiteSpace(instruction))
		{
			return false;
		}

		var trimmed = NormalizeWhitespace(instruction);
		var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if (tokens.Length < 2)
		{
			return false;
		}

		var fieldType = tokens[0].ToUpperInvariant();
		var bookmarkName = tokens[1];

		if (string.Equals(fieldType, "PAGEREF", StringComparison.Ordinal))
		{
			fieldName = "PAGEREF";
			if (bookmarkPageMap.TryGetValue(bookmarkName, out var pageNumber))
			{
				computedValue = pageNumber.ToString(CultureInfo.InvariantCulture);
				return true;
			}

			return false;
		}

		if (string.Equals(fieldType, "REF", StringComparison.Ordinal))
		{
			fieldName = "REF";
			if (bookmarkTextMap.TryGetValue(bookmarkName, out var text))
			{
				computedValue = text;
				return true;
			}

			return false;
		}

		return false;
	}
}
