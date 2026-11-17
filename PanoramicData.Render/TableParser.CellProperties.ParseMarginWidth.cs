namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	private static float ParseMarginWidth(TopMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(RightMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(BottomMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(LeftMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}
}
