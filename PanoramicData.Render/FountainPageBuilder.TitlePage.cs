namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
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
}
