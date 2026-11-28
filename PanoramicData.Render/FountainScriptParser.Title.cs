using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal static partial class FountainScriptParser
{
	private static Dictionary<string, string> ReadTitle(string[] lines, out int index)
	{
		var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string? key = null;
		index = 0;
		while (index < lines.Length)
		{
			var line = lines[index];
			if (line.Length == 0)
			{
				break;
			}

			if (key != null && IsTitleContinuation(line))
			{
				values[key] = values[key].Length == 0 ? line.Trim() : $"{values[key]}\n{line.Trim()}";
				index++;
				continue;
			}

			var match = ReadTitleField(line, index);
			if (match is null)
			{
				break;
			}

			key = match.Groups[1].Value.Trim();
			values[key] = match.Groups[2].Value.Trim();
			index++;
		}

		return values;
	}

	private static bool IsTitleContinuation(string line)
	{
		return line.StartsWith('\t') || line.StartsWith("   ", StringComparison.Ordinal);
	}

	private static Match? ReadTitleField(string line, int index)
	{
		// In a script with no title page, an initial transition is a body element.
		var match = TitleField().Match(line);
		return match.Success && (index != 0 || !IsTransition(line.Trim())) ? match : null;
	}

	[GeneratedRegex(@"^([A-Za-z][\w /'-]*):\s*(.*)$", RegexOptions.CultureInvariant)]
	private static partial Regex TitleField();
}
