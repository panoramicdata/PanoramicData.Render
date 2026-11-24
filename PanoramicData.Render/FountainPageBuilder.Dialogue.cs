namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
	private sealed record SpeechLine(string Text, float X, bool Parenthetical)
	{
		public bool EndsSentence => !Parenthetical && Text.TrimEnd('"', '\u2019', '\u201d', ')', ' ') is [.., '.' or '!' or '?' or '\u2026'];
	}

	private void AddDialogue(FountainBlock speech)
	{
		var cue = FountainLineFormatter.PlainText(speech.Text).ToUpperInvariant();
		var repeatsAfterAction = _actionSinceSpeech && string.Equals(_previousSpeaker, cue, StringComparison.Ordinal);
		_previousSpeaker = cue;
		_actionSinceSpeech = false;
		_atFirstStageAction = false;
		if (layout.IsUkStage)
		{
			AddUkDialogue(speech, repeatsAfterAction ? string.Empty : $"{cue}: ");
			return;
		}

		var label = repeatsAfterAction ? ContinuedCue(cue) : cue;
		var lines = MeasureSpeech(speech, layout.DialogueLeft, layout.DialogueRight, layout.ParentheticalLeft, layout.ParentheticalRight);
		WriteDialogue(label, cue, lines);
	}

	private void AddUkDialogue(FountainBlock speech, string prefix)
	{
		var inline = new List<string>();
		var pendingParentheticals = new List<string>();
		foreach (var part in speech.Speech ?? [])
		{
			if (part.Parenthetical)
			{
				pendingParentheticals.Add(part.Text.ToUpperInvariant());
			}
			else
			{
				inline.Add(pendingParentheticals.Count == 0 ? part.Text : $"{string.Join(' ', pendingParentheticals)} {part.Text}");
				pendingParentheticals.Clear();
			}
		}
		inline.AddRange(pendingParentheticals);

		var lines = formatter.Wrap(prefix + string.Join('\n', inline), layout.DialogueRight - layout.DialogueLeft).ToArray();
		EnsureRoom(Math.Min(2, lines.Length));
		AddLines(lines, layout.DialogueLeft);
		_baseline += _lineHeight;
	}

	private void WriteDialogue(string firstCue, string speaker, IReadOnlyList<SpeechLine> lines)
	{
		var offset = 0;
		while (offset < lines.Count)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var remaining = lines.Count - offset;
			EnsureRoom(Math.Min(remaining, NeededToStart(lines, offset)) + 1);
			var capacity = AvailableLines - 1;
			var length = Math.Min(remaining, capacity);
			if (length < remaining)
			{
				length = SelectSplit(lines, offset, capacity - (layout.HasMoreCue ? 1 : 0), AtBodyTop);
				if (length == 0)
				{
					NewPage(numbered: true);
					continue;
				}
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

	private List<SpeechLine> MeasureSpeech(FountainBlock speech, float dialogueLeft, float dialogueRight, float parentheticalLeft, float parentheticalRight)
	{
		var result = new List<SpeechLine>();
		foreach (var part in speech.Speech ?? [])
		{
			var left = part.Parenthetical ? parentheticalLeft : dialogueLeft;
			var right = part.Parenthetical ? parentheticalRight : dialogueRight;
			var text = part.Parenthetical && layout.IsTv ? part.Text.ToLowerInvariant() : part.Text;
			result.AddRange(formatter.Wrap(text, right - left).Select(line => new SpeechLine(line, left, part.Parenthetical)));
		}

		return result;
	}

	private void AddDualDialogue(FountainBlock first, FountainBlock second)
	{
		var midpoint = (layout.BodyLeft + layout.BodyRight) / 2;
		var left = MeasureSpeech(first, layout.BodyLeft, midpoint - 12, layout.BodyLeft + 12, midpoint - 12);
		var right = MeasureSpeech(second, midpoint + 12, layout.BodyRight, midpoint + 24, layout.BodyRight);
		var rows = Math.Max(left.Count, right.Count);
		EnsureRoom(rows + 1);
		if (AvailableLines < rows + 1)
		{
			AddDialogue(first);
			AddDialogue(second);
			return;
		}

		AddAtCurrentBaseline(FountainLineFormatter.PlainText(first.Text).ToUpperInvariant(), layout.BodyLeft + 36);
		AddAtCurrentBaseline(FountainLineFormatter.PlainText(second.Text).ToUpperInvariant(), midpoint + 48);
		_baseline += _lineHeight;
		for (var i = 0; i < rows; i++)
		{
			if (i < left.Count)
			{
				AddAtCurrentBaseline(left[i].Text, left[i].X);
			}
			if (i < right.Count)
			{
				AddAtCurrentBaseline(right[i].Text, right[i].X);
			}

			_baseline += _lineHeight;
		}

		_previousSpeaker = null;
		_actionSinceSpeech = false;
		_baseline += _lineHeight;
	}

	private string ContinuedCue(string speaker)
	{
		if (speaker.EndsWith(" (CONT'D)", StringComparison.Ordinal) || speaker.EndsWith(" (CONTINUED)", StringComparison.Ordinal))
		{
			return speaker;
		}

		return layout.IsUsStage ? $"{speaker} (Continued)" : $"{speaker} (CONT'D)";
	}
}
