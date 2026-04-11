namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlTabStop = DocumentFormat.OpenXml.Wordprocessing.TabStop;

/// <summary>
/// Extracts tab stop definitions from OOXML paragraph properties.
/// </summary>
internal static class TabStopParser
{
	/// <summary>
	/// Parses tab stops from paragraph properties into a <see cref="TabStopProfile"/>.
	/// </summary>
	/// <param name="pPr">The paragraph properties element, or <see langword="null"/>.</param>
	/// <returns>A <see cref="TabStopProfile"/> with the parsed tab stops, or a default profile if none are defined.</returns>
	public static TabStopProfile ParseTabStops(ParagraphProperties? pPr)
	{
		var tabs = pPr?.Tabs;
		if (tabs is null)
		{
			return new TabStopProfile([]);
		}

		var stops = new List<TabStop>();
		foreach (var tab in tabs.Elements<OoxmlTabStop>())
		{
			if (tab.Val?.Value == TabStopValues.Clear)
			{
				continue;
			}

			var position = tab.Position?.Value ?? 0;
			var type = MapTabStopType(tab.Val?.Value);
			var leader = MapTabStopLeader(tab.Leader?.Value);
			stops.Add(new TabStop(position, type, leader));
		}

		return new TabStopProfile(stops);
	}

	private static TabStopType MapTabStopType(TabStopValues? value)
	{
		if (value is null)
		{
			return TabStopType.Left;
		}

		if (value == TabStopValues.Center)
		{
			return TabStopType.Center;
		}

		if (value == TabStopValues.Right || value == TabStopValues.End)
		{
			return TabStopType.Right;
		}

		if (value == TabStopValues.Decimal || value == TabStopValues.Number)
		{
			return TabStopType.Decimal;
		}

		if (value == TabStopValues.Bar)
		{
			return TabStopType.Bar;
		}

		return TabStopType.Left;
	}

	private static TabStopLeader MapTabStopLeader(TabStopLeaderCharValues? value)
	{
		if (value is null)
		{
			return TabStopLeader.None;
		}

		if (value == TabStopLeaderCharValues.Dot)
		{
			return TabStopLeader.Dot;
		}

		if (value == TabStopLeaderCharValues.Hyphen)
		{
			return TabStopLeader.Hyphen;
		}

		if (value == TabStopLeaderCharValues.Heavy)
		{
			return TabStopLeader.Heavy;
		}

		if (value == TabStopLeaderCharValues.MiddleDot)
		{
			return TabStopLeader.MiddleDot;
		}

		if (value == TabStopLeaderCharValues.Underscore)
		{
			return TabStopLeader.Underscore;
		}

		return TabStopLeader.None;
	}
}
