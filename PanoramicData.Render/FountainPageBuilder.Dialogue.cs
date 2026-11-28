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

	private string ContinuedCue(string speaker)
	{
		if (speaker.EndsWith(" (CONT'D)", StringComparison.Ordinal) || speaker.EndsWith(" (CONTINUED)", StringComparison.Ordinal))
		{
			return speaker;
		}

		return layout.IsUsStage ? $"{speaker} (Continued)" : $"{speaker} (CONT'D)";
	}
}
