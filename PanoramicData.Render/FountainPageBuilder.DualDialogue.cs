namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
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
}
