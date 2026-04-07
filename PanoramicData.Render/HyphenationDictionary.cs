namespace PanoramicData.Render;

/// <summary>
/// Implements the Liang hyphenation algorithm using TeX-format patterns.
/// Patterns encode numeric levels between character positions; odd levels allow
/// hyphenation, even levels suppress it. When multiple patterns overlap the same
/// position, the highest level wins.
/// </summary>
internal sealed class HyphenationDictionary
{
	private readonly Dictionary<string, int[]> _patterns = new(StringComparer.Ordinal);

	/// <summary>
	/// Gets or sets the minimum number of characters before the first hyphenation point.
	/// </summary>
	public int MinPrefix { get; init; } = 2;

	/// <summary>
	/// Gets or sets the minimum number of characters after the last hyphenation point.
	/// </summary>
	public int MinSuffix { get; init; } = 2;

	/// <summary>
	/// Gets or sets the minimum word length to attempt hyphenation.
	/// </summary>
	public int MinWordLength { get; init; } = 4;

	/// <summary>
	/// Gets the number of loaded patterns.
	/// </summary>
	public int PatternCount => _patterns.Count;

	/// <summary>
	/// Adds a single TeX-format hyphenation pattern. Letters define the match string;
	/// digits between letters define the hyphenation level at that inter-character position.
	/// The dot character (.) is used to anchor patterns to the start or end of a word.
	/// </summary>
	/// <param name="texPattern">A TeX-format hyphenation pattern such as <c>hy3p</c> or <c>.ach4</c>.</param>
	public void AddPattern(string texPattern)
	{
		ArgumentNullException.ThrowIfNull(texPattern);
		ArgumentException.ThrowIfNullOrEmpty(texPattern);

		var (letters, levels) = ParsePattern(texPattern);
		_patterns[letters] = levels;
	}

	/// <summary>
	/// Loads hyphenation patterns from a text reader, one pattern per line.
	/// Empty lines and lines starting with '%' are skipped.
	/// </summary>
	/// <param name="reader">A reader providing pattern lines.</param>
	public void LoadPatterns(TextReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader);

		while (reader.ReadLine() is { } line)
		{
			if (line.Length == 0 || line[0] == '%')
			{
				continue;
			}

			AddPattern(line);
		}
	}

	/// <summary>
	/// Finds all valid hyphenation points in a word.
	/// Returns a sorted list of zero-based character indices where a hyphen may be inserted
	/// (i.e., the break is before the character at the returned index).
	/// </summary>
	/// <param name="word">The word to analyse.</param>
	/// <returns>A sorted list of valid hyphenation point indices.</returns>
	public IReadOnlyList<int> FindHyphenationPoints(string word)
	{
		ArgumentNullException.ThrowIfNull(word);

		if (word.Length < MinWordLength)
		{
			return [];
		}

		var lowerWord = word.ToLowerInvariant();

		// Wrap with word-boundary markers
		var wrapped = $".{lowerWord}.";

		// Level array: one entry per inter-character position in the wrapped string.
		// Position i sits between wrapped[i-1] and wrapped[i] (0-based).
		// We need wrapped.Length + 1 positions to cover all gaps.
		var levels = new int[wrapped.Length + 1];

		// Try every substring of wrapped against the pattern dictionary
		for (var start = 0; start < wrapped.Length; start++)
		{
			for (var length = 1; length <= wrapped.Length - start; length++)
			{
				var sub = wrapped.Substring(start, length);
				if (_patterns.TryGetValue(sub, out var patternLevels))
				{
					for (var k = 0; k < patternLevels.Length; k++)
					{
						var pos = start + k;
						if (patternLevels[k] > levels[pos])
						{
							levels[pos] = patternLevels[k];
						}
					}
				}
			}
		}

		// Convert levels to hyphenation points.
		// Position i in levels corresponds to the gap between wrapped[i-1] and wrapped[i].
		// wrapped[0] is '.', so position 1 = before first letter, position 2 = between 1st and 2nd letter, etc.
		// A hyphenation point at word index j means a break before word[j].
		// The gap between word[j-1] and word[j] is at levels position j+1 (offset by the leading dot).
		var points = new List<int>();
		for (var j = 1; j < lowerWord.Length; j++)
		{
			// levels index is j+1 because of the leading '.'
			if (levels[j + 1] % 2 != 0 && j >= MinPrefix && j <= lowerWord.Length - MinSuffix)
			{
				points.Add(j);
			}
		}

		return points;
	}

	/// <summary>
	/// Parses a TeX-format pattern string into its letter key and level array.
	/// </summary>
	private static (string Letters, int[] Levels) ParsePattern(string texPattern)
	{
		// Extract letters and digits.
		// Digits appear between letters to indicate the level at that gap.
		var letters = new List<char>();
		var levelsList = new List<int>();

		var pendingDigit = 0;
		foreach (var c in texPattern)
		{
			if (char.IsDigit(c))
			{
				pendingDigit = c - '0';
			}
			else
			{
				levelsList.Add(pendingDigit);
				pendingDigit = 0;
				letters.Add(c);
			}
		}

		// Trailing level (after the last letter)
		levelsList.Add(pendingDigit);

		return (new string([.. letters]), [.. levelsList]);
	}
}
