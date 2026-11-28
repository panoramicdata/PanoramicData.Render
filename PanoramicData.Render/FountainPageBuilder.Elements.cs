namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
	private void AddScene(string source)
	{
		_previousSpeaker = null;
		_actionSinceSpeech = false;
		var heading = FountainLineFormatter.PlainText(source).ToUpperInvariant();
		if (layout.IsStage)
		{
			AddStageScene(heading);
		}
		else
		{
			// A scene heading and at least one line of the next element stay together.
			var room = layout.IsTv ? 3 : 2;
			EnsureRoom(room);
			if (layout.IsTv && !AtBodyTop && _baseline > FountainPdfLayout.FirstBaseline + 2 * _lineHeight)
			{
				_baseline += _lineHeight;
			}
			AddLines(formatter.Wrap(heading, layout.BodyRight - layout.BodyLeft), layout.BodyLeft);
		}

		_baseline += _lineHeight;
	}

	private void AddStageScene(string heading)
	{
		BreakBodyPage();
		if (!layout.IsUkStage)
		{
			AddVisibleStageAct();
			AddLines(formatter.Wrap(heading, layout.BodyRight - 288), 288, underline: true);
		}
		else
		{
			if (_act is not null)
			{
				AddAtCurrentBaseline(_act, layout.BodyLeft);
			}

			var x = _act is null ? layout.BodyLeft : 180;
			AddLines(formatter.Wrap(heading, layout.BodyRight - x), x);
		}

		_atFirstStageAction = true;
	}

	private void AddAction(string text)
	{
		var left = layout.BodyLeft;
		if (layout.IsUsStage)
		{
			left = _atFirstStageAction ? 288 : 216;
			var trimmed = text.Trim();
			text = trimmed.StartsWith('(') && trimmed.EndsWith(')') ? trimmed : $"({trimmed})";
		}
		else if (layout.IsUkStage)
		{
			left = 252;
			text = text.ToUpperInvariant();
		}

		AddLines(formatter.Wrap(text, layout.BodyRight - left), left);
		_baseline += _lineHeight;
		_atFirstStageAction = false;
		_actionSinceSpeech = true;
	}

	private void AddTransition(string text)
	{
		var value = FountainLineFormatter.PlainText(text).ToUpperInvariant();
		var x = value == "FADE IN:" ? layout.BodyLeft : Math.Max(layout.BodyLeft, layout.BodyRight - formatter.Measure(value));
		AddLines([value], x);
		_baseline += _lineHeight;
	}
}
