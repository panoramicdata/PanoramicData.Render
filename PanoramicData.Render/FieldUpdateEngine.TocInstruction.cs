namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool TryGetTocInstruction(Paragraph paragraph, out string instruction)
	{
		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var candidateInstruction = simpleField.Instruction?.Value;
			if (ParseFieldKind(candidateInstruction) is FieldUpdateKind.Toc)
			{
				instruction = candidateInstruction ?? string.Empty;
				return true;
			}
		}

		var activeInstructions = new Stack<StringBuilder>();

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
					switch (child)
					{
						case FieldChar fieldChar when fieldChar.FieldCharType?.Value == FieldCharValues.Begin:
						activeInstructions.Push(new StringBuilder());
						break;

					case FieldCode fieldCode when activeInstructions.Count > 0:
						activeInstructions.Peek().Append(fieldCode.Text);
						break;

						case FieldChar fieldChar when activeInstructions.Count > 0 && fieldChar.FieldCharType?.Value == FieldCharValues.Separate:
						var candidateInstruction = activeInstructions.Peek().ToString();
						if (ParseFieldKind(candidateInstruction) is FieldUpdateKind.Toc)
						{
							instruction = candidateInstruction;
							return true;
						}

						break;

						case FieldChar fieldChar when activeInstructions.Count > 0 && fieldChar.FieldCharType?.Value == FieldCharValues.End:
						var completedInstruction = activeInstructions.Pop().ToString();
						if (ParseFieldKind(completedInstruction) is FieldUpdateKind.Toc)
						{
							instruction = completedInstruction;
							return true;
						}

						break;
				}
			}
		}

		instruction = string.Empty;
		return false;
	}

	private static TocSwitchSet ParseTocSwitchSet(string instruction)
	{
		var minimumLevel = 1;
		var maximumLevel = 3;
		if (TryGetQuotedSwitchValue(instruction, 'o', out var outlineLevels))
		{
			var parts = outlineLevels.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 2
				&& int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMinimum)
				&& int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMaximum)
				&& parsedMinimum <= parsedMaximum)
			{
				minimumLevel = Math.Clamp(parsedMinimum, 1, 9);
				maximumLevel = Math.Clamp(parsedMaximum, minimumLevel, 9);
			}
		}

		var separator = TryGetQuotedSwitchValue(instruction, 'p', out var customSeparator)
			? customSeparator
			: "\t";
		var customStyleLevels = ParseCustomStyleLevels(instruction);
		var isTableOfFigures = TryGetQuotedSwitchValue(instruction, 'f', out _);

		return new TocSwitchSet(
			minimumLevel,
			maximumLevel,
			!HasSwitch(instruction, 'n'),
			HasSwitch(instruction, 'h'),
			separator,
			customStyleLevels,
			isTableOfFigures);
	}

	private static IReadOnlyDictionary<string, int> ParseCustomStyleLevels(string instruction)
	{
		if (!TryGetQuotedSwitchValue(instruction, 't', out var customStyles))
		{
			return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}

		var styleLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var parts = customStyles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i + 1 < parts.Length; i += 2)
		{
			if (!int.TryParse(parts[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var level)
				|| level is < 1 or > 9)
			{
				continue;
			}

			styleLevels[parts[i]] = level;
		}

		return styleLevels;
	}

	private static bool HasSwitch(string instruction, char switchName)
		=> instruction.IndexOf($"\\{switchName}", StringComparison.OrdinalIgnoreCase) >= 0;

	private static bool TryGetQuotedSwitchValue(string instruction, char switchName, out string value)
	{
		var markerIndex = instruction.IndexOf($"\\{switchName}", StringComparison.OrdinalIgnoreCase);
		if (markerIndex < 0)
		{
			value = string.Empty;
			return false;
		}

		var valueStart = markerIndex + 2;
		while (valueStart < instruction.Length && char.IsWhiteSpace(instruction[valueStart]))
		{
			valueStart++;
		}

		if (valueStart >= instruction.Length || instruction[valueStart] != '"')
		{
			value = string.Empty;
			return false;
		}

		valueStart++;
		var valueEnd = instruction.IndexOf('"', valueStart);
		if (valueEnd < 0)
		{
			value = string.Empty;
			return false;
		}

		value = instruction[valueStart..valueEnd];
		return true;
	}
}
