namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool UpdateComplexFields(
		Paragraph paragraph,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		ISet<string> updatedFields)
	{
		var activeFields = new Stack<ActiveFieldUpdate>();
		var hasChanges = false;

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				switch (child)
				{
					case FieldCode fieldCode when activeFields.Count > 0 && !activeFields.Peek().IsResultSection:
						activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
						break;

					case FieldChar fieldChar:
						HandleFieldChar(fieldChar, activeFields);
						break;

					case Text text when activeFields.Count > 0:
						var activeField = activeFields.Peek();
						if (!activeField.IsResultSection || activeField.Kind is FieldUpdateKind.Other)
						{
							break;
						}

						if (!TryComputeFieldValue(activeField.Kind, options, propertyMap, currentPageNumber, totalPageCount, out var computedValue))
						{
							break;
						}

						if (!activeField.HasWrittenValue)
						{
							if (!string.Equals(text.Text, computedValue, StringComparison.Ordinal))
							{
								text.Text = computedValue;
								hasChanges = true;
								updatedFields.Add(GetFieldName(activeField.Kind));
							}

							activeField.HasWrittenValue = true;
						}
						else if (!string.IsNullOrEmpty(text.Text))
						{
							text.Text = string.Empty;
							hasChanges = true;
							updatedFields.Add(GetFieldName(activeField.Kind));
						}

						break;
				}
			}
		}

		return hasChanges;
	}

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

			var textNodes = simpleField.Descendants<Text>().ToArray();
			if (textNodes.Length == 0)
			{
				simpleField.AppendChild(new Run(new Text(computedValue)));
				hasChanges = true;
				updatedFields.Add(GetFieldName(kind));
				continue;
			}

			if (!string.Equals(textNodes[0].Text, computedValue, StringComparison.Ordinal))
			{
				textNodes[0].Text = computedValue;
				hasChanges = true;
				updatedFields.Add(GetFieldName(kind));
			}

			for (var i = 1; i < textNodes.Length; i++)
			{
				if (string.IsNullOrEmpty(textNodes[i].Text))
				{
					continue;
				}

				textNodes[i].Text = string.Empty;
				hasChanges = true;
				updatedFields.Add(GetFieldName(kind));
			}
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
			activeField.IsResultSection = true;
			activeField.Kind = ParseFieldKind(activeField.InstructionBuilder.ToString());
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

	private static DocumentPropertyMap BuildDocumentPropertyMap(DocxDocument doc, RenderOptions renderOptions)
		=> new(
			doc.Title ?? string.Empty,
			doc.Author ?? string.Empty,
			doc.Subject ?? string.Empty,
			doc.Keywords ?? string.Empty,
			doc.Description ?? string.Empty,
			renderOptions.SourceFilename ?? "(document)");

	private static string GetFieldName(FieldUpdateKind kind)
		=> kind switch
		{
			FieldUpdateKind.Page => "PAGE",
			FieldUpdateKind.NumPages => "NUMPAGES",
			FieldUpdateKind.Toc => "TOC",
			FieldUpdateKind.Title => "TITLE",
			FieldUpdateKind.Author => "AUTHOR",
			FieldUpdateKind.Subject => "SUBJECT",
			FieldUpdateKind.Keywords => "KEYWORDS",
			FieldUpdateKind.Description => "DESCRIPTION",
			FieldUpdateKind.Filename => "FILENAME",
			_ => string.Empty
		};

	private sealed class ActiveFieldUpdate
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldUpdateKind Kind { get; set; } = FieldUpdateKind.Other;

		public bool HasWrittenValue { get; set; }
	}

	private enum FieldUpdateKind
	{
		Other,
		Page,
		NumPages,
		Toc,
		Title,
		Author,
		Subject,
		Keywords,
		Description,
		Filename
	}
}
