namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool UpdateSimpleFields(
		Paragraph paragraph,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		ISet<string> updatedFields)
	{
		var hasChanges = false;

		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var kind = ParseFieldKind(simpleField.Instruction?.Value);
			if (kind is FieldUpdateKind.Other)
			{
				continue;
			}

			if (!TryComputeFieldValue(kind, options, propertyMap, currentPageNumber, totalPageCount, out var computedValue))
			{
				continue;
			}

			hasChanges |= WriteSimpleResult(simpleField, computedValue, GetFieldName(kind), true, updatedFields);
		}

		return hasChanges;
	}
	private static void HandleFieldChar(FieldChar fieldChar, Stack<ActiveFieldUpdate> activeFields)
	{
		if (fieldChar.FieldCharType is null)
		{
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Begin)
		{
			activeFields.Push(new ActiveFieldUpdate());
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Separate)
		{
			if (activeFields.Count == 0)
			{
				return;
			}

			var activeField = activeFields.Peek();
			activeField.BeginResult(ParseFieldKind(activeField.InstructionBuilder.ToString()));
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.End && activeFields.Count > 0)
		{
			activeFields.Pop();
		}
	}

	private static FieldUpdateKind ParseFieldKind(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return FieldUpdateKind.Other;
		}

		var trimmed = instruction.Trim();
		var firstToken = trimmed.Split([' ', '\\', '"', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();
		return firstToken switch
		{
			"PAGE" => FieldUpdateKind.Page,
			"NUMPAGES" => FieldUpdateKind.NumPages,
			"TOC" => FieldUpdateKind.Toc,
			"TITLE" => FieldUpdateKind.Title,
			"AUTHOR" => FieldUpdateKind.Author,
			"SUBJECT" => FieldUpdateKind.Subject,
			"KEYWORDS" => FieldUpdateKind.Keywords,
			"DESCRIPTION" => FieldUpdateKind.Description,
			"FILENAME" => FieldUpdateKind.Filename,
			_ => FieldUpdateKind.Other
		};
	}

	private static bool TryComputeFieldValue(
		FieldUpdateKind kind,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		out string computedValue)
	{
		computedValue = kind switch
		{
			FieldUpdateKind.Page when options.UpdatePageFields => currentPageNumber.ToString(CultureInfo.InvariantCulture),
			FieldUpdateKind.NumPages when options.UpdatePageFields => totalPageCount.ToString(CultureInfo.InvariantCulture),
			FieldUpdateKind.Title when options.UpdateDocumentProperties => propertyMap.Title,
			FieldUpdateKind.Author when options.UpdateDocumentProperties => propertyMap.Author,
			FieldUpdateKind.Subject when options.UpdateDocumentProperties => propertyMap.Subject,
			FieldUpdateKind.Keywords when options.UpdateDocumentProperties => propertyMap.Keywords,
			FieldUpdateKind.Description when options.UpdateDocumentProperties => propertyMap.Description,
			FieldUpdateKind.Filename when options.UpdateDocumentProperties => propertyMap.Filename,
			_ => string.Empty
		};

		return kind switch
		{
			FieldUpdateKind.Page or FieldUpdateKind.NumPages => options.UpdatePageFields,
			FieldUpdateKind.Title or FieldUpdateKind.Author or FieldUpdateKind.Subject or FieldUpdateKind.Keywords or FieldUpdateKind.Description or FieldUpdateKind.Filename => options.UpdateDocumentProperties,
			_ => false
		};
	}
}
