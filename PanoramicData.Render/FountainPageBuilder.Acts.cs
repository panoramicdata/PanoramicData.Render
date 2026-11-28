using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
	private void AddCenteredBlock(string text, FountainBlock? next, bool firstScriptBlock)
	{
		var value = FountainLineFormatter.PlainText(text).ToUpperInvariant();
		if (ActStart().IsMatch(value))
		{
			if (layout.IsStage)
			{
				_act = value;
				BreakBodyPage();
				if (next is not { Kind: FountainBlockKind.Scene })
				{
					AddVisibleStageAct();
				}
				return;
			}

			if (layout.IsTv)
			{
				AddTelevisionAct(value, next, firstScriptBlock);
				return;
			}
		}

		if (layout.IsTv && ActEnd().IsMatch(value))
		{
			EnsureRoom(1);
			AddCentered(value, underline: true, after: 0);
			return;
		}

		AddCentered(text);
	}

	private void AddTelevisionAct(string title, FountainBlock? next, bool firstScriptBlock)
	{
		if (!firstScriptBlock)
		{
			BreakBodyPage();
		}

		AddCentered(title, underline: true, after: 2);
		if (next is { Kind: FountainBlockKind.Scene })
		{
			AddTransition("FADE IN:");
		}
	}

	private void AddVisibleStageAct()
	{
		if (_act is not null)
		{
			AddLines([_act], layout.IsUsStage ? 288 : layout.BodyLeft, underline: true);
			_baseline += _lineHeight;
		}
	}

	[GeneratedRegex(@"^(?:ACT\s+(?:\d+|[IVXLCDM]+|[A-Z]+(?:[ -][A-Z]+)?)|TEASER|COLD OPEN|TAG)$", RegexOptions.CultureInvariant)]
	private static partial Regex ActStart();

	[GeneratedRegex(@"^END OF (?:ACT\b.+|TEASER|COLD OPEN|TAG)$", RegexOptions.CultureInvariant)]
	private static partial Regex ActEnd();
}
