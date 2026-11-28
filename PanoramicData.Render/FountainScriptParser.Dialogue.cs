using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal static partial class FountainScriptParser
{
	private static bool TryReadDialogue(string[] lines, ref int index, out FountainBlock dialogue)
	{
		dialogue = null!;
		var cue = lines[index].Trim();
		if (cue.StartsWith('!') || index + 1 >= lines.Length || IsBoundary(lines[index + 1]))
		{
			return false;
		}

		var dual = cue.EndsWith('^');
		if (dual)
		{
			cue = cue[..^1].TrimEnd();
		}

		cue = ReadCharacterCue(lines, index, cue);
		if (cue.Length == 0)
		{
			return false;
		}

		dialogue = new FountainBlock(FountainBlockKind.Dialogue, cue, ReadSpeech(lines, ref index), dual);
		return true;
	}

	private static List<FountainSpeechLine> ReadSpeech(string[] lines, ref int index)
	{
		var speech = new List<FountainSpeechLine>();
		index++;
		while (index < lines.Length && !IsBoundary(lines[index]))
		{
			var line = lines[index++].Trim();
			speech.Add(new FountainSpeechLine(line, line.StartsWith('(') && line.EndsWith(')')));
		}

		return speech;
	}

	private static string ReadCharacterCue(string[] lines, int index, string cue)
	{
		if (cue.StartsWith('@'))
		{
			return cue[1..].Trim();
		}

		var beginsBlock = index == 0 || IsBoundary(lines[index - 1]);
		return beginsBlock && IsUpperCue(cue) && !SceneHeading().IsMatch(cue) && !IsTransition(cue) && !cue.StartsWith('>')
			? cue
			: string.Empty;
	}

	private static bool IsUpperCue(string cue)
	{
		var extension = cue.IndexOf('(');
		var name = extension < 0 ? cue : cue[..extension];
		return name.Any(char.IsUpper) && !name.Any(char.IsLower)
			&& char.IsLetterOrDigit(name[0]);
	}
}
