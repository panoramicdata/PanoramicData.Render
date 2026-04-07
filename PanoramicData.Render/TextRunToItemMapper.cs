using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>
/// Maps text content to Knuth-Plass items for line breaking.
/// Words become boxes, spaces become glue, and hyphens become penalty-delimited boxes.
/// </summary>
internal sealed class TextRunToItemMapper
{
	/// <summary>
	/// Default hyphen penalty cost. Positive values discourage hyphen breaks.
	/// </summary>
	private const float HyphenPenalty = 50f;

	private readonly MeasurementEngine _engine;
	private readonly HyphenationDictionary? _hyphenation;

	/// <summary>
	/// Initializes a new instance of the <see cref="TextRunToItemMapper"/> class.
	/// </summary>
	/// <param name="engine">The measurement engine for computing glyph widths.</param>
	/// <param name="hyphenation">An optional hyphenation dictionary for automatic hyphenation.</param>
	public TextRunToItemMapper(MeasurementEngine engine, HyphenationDictionary? hyphenation = null)
	{
		ArgumentNullException.ThrowIfNull(engine);
		_engine = engine;
		_hyphenation = hyphenation;
	}

	/// <summary>
	/// Maps a sequence of run elements to Knuth-Plass items.
	/// Handles text, breaks, and tab elements.
	/// </summary>
	/// <param name="elements">The run elements to map.</param>
	/// <param name="typeface">The typeface for measuring widths.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <returns>A list of Knuth-Plass items.</returns>
	public IReadOnlyList<KnuthPlassItem> MapRunElements(IReadOnlyList<RunElement> elements, SKTypeface typeface, float fontSizePoints)
	{
		ArgumentNullException.ThrowIfNull(elements);
		ArgumentNullException.ThrowIfNull(typeface);

		if (fontSizePoints <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontSizePoints));
		}

		var items = new List<KnuthPlassItem>();

		foreach (var element in elements)
		{
			switch (element)
			{
				case TextRunElement textElement:
					var textItems = MapTextRun(textElement.Text, typeface, fontSizePoints);
					items.AddRange(textItems);
					break;

				case BreakRunElement breakElement:
					items.Add(new KnuthPlassPenalty(0f, KnuthPlassPenalty.NegativeInfinity)
					{
						BreakType = breakElement.BreakType
					});
					break;

				case TabRunElement:
					// Treat tab as a space-like glue; actual tab stop handling is in step 2.3.5
					var tabItems = MapTextRun("\t", typeface, fontSizePoints);
					items.AddRange(tabItems);
					break;

				case NonBreakingHyphenRunElement:
					// Non-breaking hyphen: renders as a hyphen but never a break opportunity
					var hyphenWidth = MeasureWordWidth("-", typeface, fontSizePoints);
					items.Add(new KnuthPlassBox(hyphenWidth));
					break;
			}
		}

		return items;
	}

	/// <summary>
	/// Maps a text string to a sequence of Knuth-Plass items.
	/// </summary>
	/// <param name="text">The text content to map.</param>
	/// <param name="typeface">The typeface for measuring widths.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <returns>A list of Knuth-Plass items representing the text.</returns>
	public IReadOnlyList<KnuthPlassItem> MapTextRun(string text, SKTypeface typeface, float fontSizePoints)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(typeface);

		if (fontSizePoints <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontSizePoints));
		}

		if (text.Length == 0)
		{
			return [];
		}

		var items = new List<KnuthPlassItem>();
		var tokens = Tokenize(text);

		foreach (var token in tokens)
		{
			switch (token.Type)
			{
				case TokenType.Word:
					AddWordItems(items, token.Text, typeface, fontSizePoints);
					break;
				case TokenType.Space:
					AddSpaceGlue(items, token.Text, typeface, fontSizePoints);
					break;
			}
		}

		return items;
	}

	/// <summary>
	/// Adds box items for a word, splitting at hyphens to create penalty break opportunities.
	/// When automatic hyphenation is enabled, also inserts discretionary hyphen penalties.
	/// </summary>
	private void AddWordItems(List<KnuthPlassItem> items, string word, SKTypeface typeface, float fontSizePoints)
	{
		// Split on explicit hyphens to create break opportunities
		var parts = SplitOnHyphens(word);

		for (var i = 0; i < parts.Count; i++)
		{
			var part = parts[i];

			// Apply automatic hyphenation to non-hyphen-terminated parts
			if (_hyphenation is not null && !part.EndsWith('-'))
			{
				AddHyphenatedWordItems(items, part, typeface, fontSizePoints);
			}
			else
			{
				var width = MeasureWordWidth(part, typeface, fontSizePoints);
				items.Add(new KnuthPlassBox(width));
			}

			// Add a penalty after each hyphen-terminated part (except the last part)
			if (i < parts.Count - 1)
			{
				// The hyphen is already included in the box, so penalty width is 0
				items.Add(new KnuthPlassPenalty(0f, HyphenPenalty, isFlagged: true));
			}
		}
	}

	/// <summary>
	/// Splits a word at hyphenation points and inserts discretionary hyphen penalties.
	/// The penalty width is the width of a hyphen character (the hyphen would be rendered
	/// at the end of the line if the break is taken).
	/// </summary>
	private void AddHyphenatedWordItems(List<KnuthPlassItem> items, string word, SKTypeface typeface, float fontSizePoints)
	{
		var points = _hyphenation!.FindHyphenationPoints(word);

		if (points.Count == 0)
		{
			var width = MeasureWordWidth(word, typeface, fontSizePoints);
			items.Add(new KnuthPlassBox(width));
			return;
		}

		var hyphenWidth = MeasureWordWidth("-", typeface, fontSizePoints);
		var start = 0;

		for (var i = 0; i < points.Count; i++)
		{
			var point = points[i];
			var segment = word[start..point];
			var segWidth = MeasureWordWidth(segment, typeface, fontSizePoints);
			items.Add(new KnuthPlassBox(segWidth));
			items.Add(new KnuthPlassPenalty(hyphenWidth, HyphenPenalty, isFlagged: true));
			start = point;
		}

		// Add the remaining segment
		var remainder = word[start..];
		var remainderWidth = MeasureWordWidth(remainder, typeface, fontSizePoints);
		items.Add(new KnuthPlassBox(remainderWidth));
	}

	/// <summary>
	/// Adds a glue item for a space sequence.
	/// </summary>
	private void AddSpaceGlue(List<KnuthPlassItem> items, string spaces, SKTypeface typeface, float fontSizePoints)
	{
		var spaceWidth = MeasureWordWidth(spaces, typeface, fontSizePoints);

		// Standard inter-word glue: stretch by 1/2, shrink by 1/3
		var stretch = spaceWidth / 2f;
		var shrink = spaceWidth / 3f;

		items.Add(new KnuthPlassGlue(spaceWidth, stretch, shrink));
	}

	/// <summary>
	/// Measures the total width of a text string in twips.
	/// </summary>
	private float MeasureWordWidth(string text, SKTypeface typeface, float fontSizePoints)
	{
		var advances = _engine.MeasureGlyphAdvancesInTwips(typeface, fontSizePoints, text);
		var total = 0f;
		for (var i = 0; i < advances.Count; i++)
		{
			total += advances[i];
		}

		return total;
	}

	/// <summary>
	/// Tokenizes text into word and space sequences.
	/// Tab characters are treated as spaces.
	/// </summary>
	private static List<Token> Tokenize(string text)
	{
		var tokens = new List<Token>();
		var i = 0;

		while (i < text.Length)
		{
			if (IsSpace(text[i]))
			{
				var start = i;
				while (i < text.Length && IsSpace(text[i]))
				{
					i++;
				}

				tokens.Add(new Token(TokenType.Space, text[start..i]));
			}
			else
			{
				var start = i;
				while (i < text.Length && !IsSpace(text[i]))
				{
					i++;
				}

				tokens.Add(new Token(TokenType.Word, text[start..i]));
			}
		}

		return tokens;
	}

	/// <summary>
	/// Splits a word on hyphens, keeping the hyphen attached to the preceding part.
	/// For example, "well-known" becomes ["well-", "known"].
	/// A trailing hyphen (e.g., "well-") stays as a single part ["well-"].
	/// </summary>
	private static List<string> SplitOnHyphens(string word)
	{
		var parts = new List<string>();
		var start = 0;

		for (var i = 0; i < word.Length; i++)
		{
			if (word[i] == '-' && i < word.Length - 1)
			{
				// Include the hyphen in the current part
				parts.Add(word[start..(i + 1)]);
				start = i + 1;
			}
		}

		// Add remaining text
		if (start < word.Length)
		{
			parts.Add(word[start..]);
		}

		return parts;
	}

	/// <summary>
	/// Returns true if the character is a space or tab.
	/// Non-breaking space (U+00A0) is explicitly excluded — it prevents line breaks.
	/// </summary>
	private static bool IsSpace(char c) => c is ' ' or '\t';

	/// <summary>
	/// Represents a token type.
	/// </summary>
	private enum TokenType
	{
		/// <summary>A word (non-space content).</summary>
		Word,

		/// <summary>A space sequence.</summary>
		Space
	}

	/// <summary>
	/// Represents a tokenized segment of text.
	/// </summary>
	/// <param name="Type">The token type.</param>
	/// <param name="Text">The token text content.</param>
	private readonly record struct Token(TokenType Type, string Text);
}
