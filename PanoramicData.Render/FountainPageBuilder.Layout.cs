using System.Globalization;

namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
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
}
