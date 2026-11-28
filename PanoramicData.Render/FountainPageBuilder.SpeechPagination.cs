namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
	private void WriteDialogue(string firstCue, string speaker, IReadOnlyList<SpeechLine> lines)
	{
		var offset = 0;
		while (offset < lines.Count)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var remaining = lines.Count - offset;
			EnsureRoom(Math.Min(remaining, NeededToStart(lines, offset)) + 1);
			var length = SelectSpeechChunk(lines, offset, remaining);
			if (length == 0)
			{
				NewPage(numbered: true);
				continue;
			}

			AddAtCurrentBaseline(offset == 0 ? firstCue : ContinuedCue(speaker), layout.CueLeft);
			_baseline += _lineHeight;
			for (var i = offset; i < offset + length; i++)
			{
				AddAtCurrentBaseline(lines[i].Text, lines[i].X);
				_baseline += _lineHeight;
			}

			offset += length;
			if (offset < lines.Count)
			{
				if (layout.HasMoreCue)
				{
					AddAtCurrentBaseline("(MORE)", layout.CueLeft);
				}

				NewPage(numbered: true);
			}
		}

		_baseline += _lineHeight;
	}

	private int SelectSpeechChunk(IReadOnlyList<SpeechLine> lines, int offset, int remaining)
	{
		var capacity = AvailableLines - 1;
		return remaining <= capacity ? remaining : SelectSplit(lines, offset, capacity - (layout.HasMoreCue ? 1 : 0), AtBodyTop);
	}

	private static int NeededToStart(IReadOnlyList<SpeechLine> lines, int offset)
	{
		var visible = 0;
		var index = offset;
		while (index < lines.Count && visible < 2)
		{
			if (!lines[index].Parenthetical)
			{
				visible++;
			}

			index++;
		}

		return index - offset;
	}

	private static int SelectSplit(IReadOnlyList<SpeechLine> lines, int offset, int capacity, bool atTop)
	{
		var safePosition = 0;
		var sentencePosition = 0;
		var dialogueRows = 0;
		var end = Math.Min(offset + capacity, lines.Count - 1);
		for (var index = offset; index < end; index++)
		{
			var line = lines[index];
			if (line.Parenthetical || ++dialogueRows < 2)
			{
				continue;
			}

			safePosition = index - offset + 1;
			if (line.EndsSentence)
			{
				sentencePosition = safePosition;
			}
		}

		// Prefer the final complete sentence. A speech longer than a whole page may
		// contain no sentence boundary, so make progress without dropping text.
		return sentencePosition > 0 ? sentencePosition : atTop ? Math.Max(1, safePosition == 0 ? capacity : safePosition) : 0;
	}
}
