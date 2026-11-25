using System.Text.RegularExpressions;
using SkiaSharp;

namespace PanoramicData.Render;

internal sealed partial class FountainLineFormatter(SKFont font)
{
	public float Measure(string value) => font.MeasureText(value);

	public IEnumerable<string> Wrap(string value, float width, float firstLineInset = 0)
	{
		foreach (var sourceLine in value.Split('\n'))
		{
			var text = PlainText(sourceLine).Replace("\t", "    ", StringComparison.Ordinal);
			if (Measure(text) <= width - firstLineInset)
			{
				yield return text;
			}
			else
			{
				foreach (var line in WrapLine(text, width, firstLineInset))
				{
					yield return line;
				}
			}

			firstLineInset = 0;
		}
	}

	public static string PlainText(string source) =>
		Escaped().Replace(Emphasis().Replace(source, "$2"), "$1");

	private IEnumerable<string> WrapLine(string text, float width, float firstLineInset)
	{
		var tokens = WordsAndSpaces().Matches(text);
		var items = new List<KnuthPlassItem>(tokens.Count + 2);
		foreach (Match token in tokens)
		{
			var measured = Measure(token.Value);
			items.Add(char.IsWhiteSpace(token.Value[0])
				? new KnuthPlassGlue(measured, measured * 4, 0)
				: new KnuthPlassBox(measured));
		}

		items.Add(new KnuthPlassGlue(0, width * 100, 0));
		items.Add(new KnuthPlassPenalty(0, KnuthPlassPenalty.NegativeInfinity));

		foreach (var line in KnuthPlassAlgorithm.FindBreaks(items, index => index == 0 ? width - firstLineInset : width))
		{
			var from = Math.Min(line.StartIndex, tokens.Count);
			var to = Math.Min(line.EndIndex + 1, tokens.Count);
			var result = string.Concat(tokens.Skip(from).Take(to - from).Select(static token => token.Value)).TrimEnd();
			if (result.Length > 0)
			{
				yield return result;
			}
		}
	}

	[GeneratedRegex(@"\S+|\s+", RegexOptions.CultureInvariant)]
	private static partial Regex WordsAndSpaces();

	[GeneratedRegex(@"(?<!\\)(\*{1,3}|_)(?!\s)((?:\\.|.)+?)(?<!\s)(?<!\\)\1", RegexOptions.CultureInvariant)]
	private static partial Regex Emphasis();

	[GeneratedRegex(@"\\([*_\\])", RegexOptions.CultureInvariant)]
	private static partial Regex Escaped();
}
