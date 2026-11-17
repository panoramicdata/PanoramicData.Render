namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
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
