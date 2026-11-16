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
						if (!activeField.IsResultSection)
						{
							break;
						}

						if (!TryParseSeqInstruction(activeField.InstructionBuilder.ToString(), out var identifier, out var resetValue, out var isHidden))
						{
							break;
						}

						var seqValue = ComputeSequenceValue(counters, identifier, resetValue);
						if (!activeField.HasWrittenValue)
						{
							var displayValue = isHidden ? string.Empty : seqValue.ToString(CultureInfo.InvariantCulture);
							if (!string.Equals(text.Text, displayValue, StringComparison.Ordinal))
							{
								text.Text = displayValue;
								hasChanges = true;
								updatedFields.Add("SEQ");
							}

							activeField.HasWrittenValue = true;
						}
						else if (!string.IsNullOrEmpty(text.Text))
						{
							text.Text = string.Empty;
							hasChanges = true;
							updatedFields.Add("SEQ");
						}

						break;
				}
			}
		}

		return hasChanges;
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

			var textNodes = simpleField.Descendants<Text>().ToArray();
			if (textNodes.Length == 0)
			{
				if (!isHidden)
				{
					simpleField.AppendChild(new Run(new Text(displayValue)));
					hasChanges = true;
					updatedFields.Add("SEQ");
				}

				continue;
			}

			if (!string.Equals(textNodes[0].Text, displayValue, StringComparison.Ordinal))
			{
				textNodes[0].Text = displayValue;
				hasChanges = true;
				updatedFields.Add("SEQ");
			}

			for (var i = 1; i < textNodes.Length; i++)
			{
				if (!string.IsNullOrEmpty(textNodes[i].Text))
				{
					textNodes[i].Text = string.Empty;
					hasChanges = true;
					updatedFields.Add("SEQ");
				}
			}
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

		var trimmed = NormalizeWhitespace(instruction);
		var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if (tokens.Length < 2 || !string.Equals(tokens[0], "SEQ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		identifier = tokens[1];

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

		return true;
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
