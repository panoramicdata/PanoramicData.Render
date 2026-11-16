namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool UpdateSequenceFields(
		Body documentBody,
		FieldUpdateOptions options,
		ISet<string> updatedFields)
	{
		if (!options.UpdateSequenceFields)
		{
			return false;
		}

		var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var hasChanges = false;

		foreach (var paragraph in documentBody.Descendants<Paragraph>())
		{
			hasChanges |= UpdateSequenceComplexFieldsInParagraph(paragraph, counters, updatedFields);
			hasChanges |= UpdateSequenceSimpleFieldsInParagraph(paragraph, counters, updatedFields);
		}

		return hasChanges;
	}

	private static bool UpdateSequenceComplexFieldsInParagraph(
		Paragraph paragraph,
		Dictionary<string, int> counters,
		ISet<string> updatedFields)
		=> ProcessComplexFieldResults(
			paragraph,
			(text, activeField) => UpdateSequenceResultText(text, activeField, counters, updatedFields));

	private static bool UpdateSequenceResultText(
		Text text,
		ActiveFieldUpdate activeField,
		Dictionary<string, int> counters,
		ISet<string> updatedFields)
	{
		if (!activeField.IsResultSection)
		{
			return false;
		}

		if (!TryParseSeqInstruction(activeField.InstructionBuilder.ToString(), out var identifier, out var resetValue, out var isHidden))
		{
			return false;
		}

		var seqValue = ComputeSequenceValue(counters, identifier, resetValue);
		var displayValue = isHidden ? string.Empty : seqValue.ToString(CultureInfo.InvariantCulture);
		return WriteComplexResult(text, activeField, displayValue, "SEQ", updatedFields);
	}

	private static bool UpdateSequenceSimpleFieldsInParagraph(
		Paragraph paragraph,
		Dictionary<string, int> counters,
		ISet<string> updatedFields)
	{
		var hasChanges = false;

		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var instruction = simpleField.Instruction?.Value;
			if (!TryParseSeqInstruction(instruction, out var identifier, out var resetValue, out var isHidden))
			{
				continue;
			}

			var seqValue = ComputeSequenceValue(counters, identifier, resetValue);
			var displayValue = isHidden ? string.Empty : seqValue.ToString(CultureInfo.InvariantCulture);
			hasChanges |= WriteSimpleResult(simpleField, displayValue, "SEQ", !isHidden, updatedFields);
		}

		return hasChanges;
	}
	private static bool TryParseSeqInstruction(
		string? instruction,
		out string identifier,
		out int? resetValue,
		out bool isHidden)
	{
		identifier = string.Empty;
		resetValue = null;
		isHidden = false;

		if (string.IsNullOrWhiteSpace(instruction))
		{
			return false;
		}

		var tokens = NormalizeWhitespace(instruction).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length < 2 || !string.Equals(tokens[0], "SEQ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		identifier = tokens[1];
		ParseSeqSwitches(tokens, out resetValue, out isHidden);
		return true;
	}

	private static void ParseSeqSwitches(string[] tokens, out int? resetValue, out bool isHidden)
	{
		resetValue = null;
		isHidden = false;

		var skipNext = false;
		for (var i = 2; i < tokens.Length; i++)
		{
			if (skipNext)
			{
				skipNext = false;
				continue;
			}

			if (string.Equals(tokens[i], "\\r", StringComparison.Ordinal) && i + 1 < tokens.Length)
			{
				if (int.TryParse(tokens[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var resetVal))
				{
					resetValue = resetVal;
				}

				skipNext = true;
			}
			else if (string.Equals(tokens[i], "\\h", StringComparison.Ordinal))
			{
				isHidden = true;
			}
		}
	}
	private static int ComputeSequenceValue(
		Dictionary<string, int> counters,
		string identifier,
		int? resetValue)
	{
		if (resetValue.HasValue)
		{
			counters[identifier] = resetValue.Value;
			return resetValue.Value;
		}

		if (!counters.TryGetValue(identifier, out var current))
		{
			current = 0;
		}

		current++;
		counters[identifier] = current;
		return current;
	}
}
