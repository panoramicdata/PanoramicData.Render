using System.Globalization;
using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder(
	FountainPdfLayout layout,
	FountainLineFormatter formatter,
	CancellationToken cancellationToken)
{
	private const float _lineHeight = FountainPdfLayout.LineHeight;
	private readonly List<FountainPdfPage> _pages = [];
	private float _baseline = FountainPdfLayout.FirstBaseline;
	private int _pageNumber;
	private string? _act;
	private string? _previousSpeaker;
	private bool _atFirstStageAction;
	private bool _actionSinceSpeech;

	private FountainPdfPage Page => _pages[^1];

	public IReadOnlyList<FountainPdfPage> Build(FountainScript script)
	{
		AddTitlePage(script.TitlePage);
		var firstBodyBlock = layout.IsStage ? AddStageFrontMatter(script.Blocks) : 0;
		if (firstBodyBlock == script.Blocks.Count && _pages.Count > 0)
		{
			return _pages;
		}

		NewPage(numbered: true);
		if (layout.IsTv && script.TitlePage.GetValueOrDefault("Title") is { Length: > 0 } title && firstBodyBlock < script.Blocks.Count)
		{
			AddCentered(title.ToUpperInvariant(), underline: true, after: 2);
		}

		for (var index = firstBodyBlock; index < script.Blocks.Count; index++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			AddBlock(script.Blocks, ref index);
		}

		return _pages;
	}

	private void AddBlock(IReadOnlyList<FountainBlock> blocks, ref int index)
	{
		var block = blocks[index];
		var next = blocks.ElementAtOrDefault(index + 1);
		switch (block.Kind)
		{
			case FountainBlockKind.PageBreak:
				BreakBodyPage();
				break;
			case FountainBlockKind.Centered:
				AddCenteredBlock(block.Text, next, index == 0);
				break;
			case FountainBlockKind.Scene:
				AddScene(block.Text);
				break;
			case FountainBlockKind.Dialogue:
				index += AddDialogueBlock(block, next);
				break;
			case FountainBlockKind.Transition:
				AddTransition(block.Text);
				break;
			case FountainBlockKind.Lyric:
			case FountainBlockKind.Action:
				AddAction(block.Text);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(blocks));
		}
	}

	private int AddDialogueBlock(FountainBlock block, FountainBlock? next)
	{
		if (!layout.IsStage && next is { Kind: FountainBlockKind.Dialogue, DualDialogue: true })
		{
			AddDualDialogue(block, next);
			return 1;
		}

		AddDialogue(block);
		return 0;
	}

	private void AddTitlePage(IReadOnlyDictionary<string, string> title)
	{
		string[] centerKeys = ["Title", "Credit", "Author", "Authors", "Source"];
		string[] cornerKeys = ["Contact", "Draft date", "Copyright"];
		var centered = ReadTitleValues(title, centerKeys);
		var contact = ReadTitleValues(title, cornerKeys);
		if (centered.Count == 0 && contact.Count == 0)
		{
			return;
		}

		NewPage(numbered: false);
		_baseline = layout.IsTv ? 280 : layout.PageHeight * 0.34f;
		foreach (var text in centered)
		{
			AddCentered(text, after: 1);
		}

		var details = contact.Count == 0 ? [] : formatter.Wrap(string.Join('\n', contact), layout.PageWidth - 144).ToArray();
		_baseline = Math.Max(_baseline, layout.PageHeight - 60 - details.Length * _lineHeight);
		foreach (var line in details)
		{
			AddAtCurrentBaseline(line, 72);
			_baseline += _lineHeight;
		}
	}

	private static List<string> ReadTitleValues(IReadOnlyDictionary<string, string> title, IEnumerable<string> keys) =>
		keys.Select(key => title.GetValueOrDefault(key))
			.OfType<string>()
			.Where(static value => value.Length > 0)
			.ToList();

	private int AddStageFrontMatter(IReadOnlyList<FountainBlock> blocks)
	{
		var index = 0;
		var priorHeading = string.Empty;
		while (ReadFrontMatterName(blocks.ElementAtOrDefault(index)) is { } name)
		{
			var cast = name is "CAST" or "CHARACTERS";
			if (priorHeading.Length == 0 || layout.IsUsStage && priorHeading is "CAST" or "CHARACTERS")
			{
				NewPage(numbered: false);
			}

			AddCentered(cast && layout.IsUsStage ? "CHARACTERS" : name, after: 2);
			index++;
			AddFrontMatterParagraphs(blocks, ref index, cast);

			priorHeading = name;
		}

		return index;
	}

	private static string? ReadFrontMatterName(FountainBlock? block)
	{
		if (block is not { Kind: FountainBlockKind.Centered })
		{
			return null;
		}

		var name = FountainLineFormatter.PlainText(block.Text).ToUpperInvariant();
		return FrontMatter().IsMatch(name) ? name : null;
	}

	private void AddFrontMatterParagraphs(IReadOnlyList<FountainBlock> blocks, ref int index, bool cast)
	{
		while (index < blocks.Count && blocks[index] is { Kind: FountainBlockKind.Action } action)
		{
			foreach (var line in action.Text.Split('\n'))
			{
				AddFrontMatterLine(line, cast);
			}
			_baseline += _lineHeight;
			index++;
		}
	}

	private void AddFrontMatterLine(string text, bool cast)
	{
		var separator = cast ? text.IndexOf(" - ", StringComparison.Ordinal) : -1;
		if (separator < 0 || !layout.IsUsStage || formatter.Measure(text[..separator]) > 170)
		{
			AddLines(formatter.Wrap(text, layout.PageWidth - 144), 72);
			return;
		}

		EnsureRoom(1);
		AddAtCurrentBaseline(FountainLineFormatter.PlainText(text[..separator]).ToUpperInvariant(), 72);
		AddLines(formatter.Wrap(text[(separator + 3)..], layout.PageWidth - 324), 252);
	}

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

	private void AddCentered(string text, bool underline = false, int after = 1)
	{
		foreach (var line in formatter.Wrap(text, layout.PageWidth - 144))
		{
			EnsureRoom(1);
			AddAtCurrentBaseline(line, (layout.PageWidth - formatter.Measure(line)) / 2, underline);
			_baseline += _lineHeight;
		}

		_baseline += after * _lineHeight;
	}

	private void AddLines(IEnumerable<string> lines, float x, bool underline = false)
	{
		foreach (var line in lines)
		{
			cancellationToken.ThrowIfCancellationRequested();
			EnsureRoom(1);
			AddAtCurrentBaseline(line, x, underline);
			_baseline += _lineHeight;
		}
	}

	private bool AtBodyTop => _baseline == FountainPdfLayout.FirstBaseline;
	private int AvailableLines => Math.Max(0, 1 + (int)MathF.Floor((layout.BodyBottom - _baseline) / _lineHeight));

	private void EnsureRoom(int lines)
	{
		if (lines > AvailableLines && !AtBodyTop)
		{
			NewPage(Page.BodyPageNumber.HasValue);
		}
	}

	private void BreakBodyPage()
	{
		if (!AtBodyTop && (Page.Lines.Count > 0 || _baseline > FountainPdfLayout.FirstBaseline))
		{
			NewPage(numbered: true);
		}
	}

	private void NewPage(bool numbered)
	{
		_pages.Add(new FountainPdfPage([], numbered ? ++_pageNumber : null));
		_baseline = FountainPdfLayout.FirstBaseline;
		if (!numbered || layout.Profile == FountainRenderProfile.FeatureScreenplay && _pageNumber == 1)
		{
			return;
		}

		var number = _pageNumber.ToString(CultureInfo.InvariantCulture);
		var label = layout.IsUkStage ? $"-{number}-" : $"{number}.";
		var width = formatter.Measure(label);
		var x = layout.NumberX - (layout.IsUkStage ? width / 2 : width);
		Page.Lines.Add(new FountainPdfLine(label, x, layout.NumberY));
	}

	private void AddAtCurrentBaseline(string text, float x, bool underline = false)
	{
		if (text.Length > 0)
		{
			Page.Lines.Add(new FountainPdfLine(text, x, _baseline, underline));
		}
	}

	[GeneratedRegex(@"^(?:ACT\s+(?:\d+|[IVXLCDM]+|[A-Z]+(?:[ -][A-Z]+)?)|TEASER|COLD OPEN|TAG)$", RegexOptions.CultureInvariant)]
	private static partial Regex ActStart();

	[GeneratedRegex(@"^END OF (?:ACT\b.+|TEASER|COLD OPEN|TAG)$", RegexOptions.CultureInvariant)]
	private static partial Regex ActEnd();

	[GeneratedRegex(@"^(?:CAST|CHARACTERS|SETTING|TIME)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex FrontMatter();
}
