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
		=> ProcessComplexFieldResults(
			paragraph,
			(text, activeField) => UpdateComputedResultText(text, activeField, options, propertyMap, currentPageNumber, totalPageCount, updatedFields));

	private static bool UpdateComputedResultText(
		Text text,
		ActiveFieldUpdate activeField,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		ISet<string> updatedFields)
	{
		if (!activeField.IsResultSection || activeField.Kind is FieldUpdateKind.Other)
		{
			return false;
		}

		if (!TryComputeFieldValue(activeField.Kind, options, propertyMap, currentPageNumber, totalPageCount, out var computedValue))
		{
			return false;
		}

		return WriteComplexResult(text, activeField, computedValue, GetFieldName(activeField.Kind), updatedFields);
	}

	private static bool ProcessComplexFieldResults(Paragraph paragraph, Func<Text, ActiveFieldUpdate, bool> handleResultText)
	{
		var activeFields = new Stack<ActiveFieldUpdate>();
		var hasChanges = false;

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				if (child is FieldCode fieldCode && activeFields.Count > 0 && !activeFields.Peek().IsResultSection)
				{
					activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
				}
				else if (child is FieldChar fieldChar)
				{
					HandleFieldChar(fieldChar, activeFields);
				}
				else if (child is Text text && activeFields.Count > 0)
				{
					hasChanges |= handleResultText(text, activeFields.Peek());
				}
			}
		}

		return hasChanges;
	}

	private static bool WriteComplexResult(Text text, ActiveFieldUpdate activeField, string value, string fieldName, ISet<string> updatedFields)
	{
		if (!activeField.HasWrittenValue)
		{
			activeField.HasWrittenValue = true;
			if (string.Equals(text.Text, value, StringComparison.Ordinal))
			{
				return false;
			}

			text.Text = value;
			updatedFields.Add(fieldName);
			return true;
		}

		if (string.IsNullOrEmpty(text.Text))
		{
			return false;
		}

		text.Text = string.Empty;
		updatedFields.Add(fieldName);
		return true;
	}

	private static bool WriteSimpleResult(SimpleField simpleField, string value, string fieldName, bool appendWhenMissing, ISet<string> updatedFields)
	{
		var textNodes = simpleField.Descendants<Text>().ToArray();
		if (textNodes.Length == 0)
		{
			if (!appendWhenMissing)
			{
				return false;
			}

			simpleField.AppendChild(new Run(new Text(value)));
			updatedFields.Add(fieldName);
			return true;
		}

		var hasChanges = false;
		if (!string.Equals(textNodes[0].Text, value, StringComparison.Ordinal))
		{
			textNodes[0].Text = value;
			hasChanges = true;
		}

		foreach (var extra in textNodes.Skip(1).Where(n => !string.IsNullOrEmpty(n.Text)))
		{
			extra.Text = string.Empty;
			hasChanges = true;
		}

		if (hasChanges)
		{
			updatedFields.Add(fieldName);
		}

		return hasChanges;
	}
}
