namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

internal static partial class RunElementParser
{
	internal static AnchorPlacementInfo ParseAnchorPlacement(DW.Anchor anchor)
	{
		ArgumentNullException.ThrowIfNull(anchor);

		var horizontalPosition = anchor.GetFirstChild<DW.HorizontalPosition>();
		var verticalPosition = anchor.GetFirstChild<DW.VerticalPosition>();

		return new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = ParseHorizontalRelativeFrom(horizontalPosition?.RelativeFrom?.Value),
			VerticalRelativeFrom = ParseVerticalRelativeFrom(verticalPosition?.RelativeFrom?.Value),
			HorizontalOffsetEmu = ParseOffset(horizontalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			VerticalOffsetEmu = ParseOffset(verticalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			HorizontalAlignment = ParseHorizontalAlignment(horizontalPosition?.GetFirstChild<DW.HorizontalAlignment>()?.InnerText),
			VerticalAlignment = ParseVerticalAlignment(verticalPosition?.GetFirstChild<DW.VerticalAlignment>()?.InnerText),
			BehindDocument = ParseOnOffValue(anchor.BehindDoc),
			WrapStyle = ParseWrapStyle(anchor),
			DistanceTopEmu = anchor.DistanceFromTop?.Value ?? 0U,
			DistanceBottomEmu = anchor.DistanceFromBottom?.Value ?? 0U,
			DistanceLeftEmu = anchor.DistanceFromLeft?.Value ?? 0U,
			DistanceRightEmu = anchor.DistanceFromRight?.Value ?? 0U
		};
	}

	private static AnchorWrapStyle ParseWrapStyle(DW.Anchor anchor)
	{
		foreach (var child in anchor.ChildElements)
		{
			var style = child.LocalName switch
			{
				"wrapSquare" => AnchorWrapStyle.Square,
				"wrapTight" or "wrapThrough" => AnchorWrapStyle.Tight,
				"wrapTopAndBottom" or "wrapTopBottom" => AnchorWrapStyle.TopAndBottom,
				"wrapNone" => AnchorWrapStyle.None,
				_ => (AnchorWrapStyle?)null
			};

			if (style is not null)
			{
				return style.Value;
			}
		}

		return AnchorWrapStyle.None;
	}

	private static int ParsePercentage(Int32Value? value)
	{
		if (value is null)
		{
			return 0;
		}

		return value.Value;
	}

	private static long ParseOffset(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0;
		}

		return long.TryParse(value, out var parsed) ? parsed : 0;
	}

	private static AnchorRelativeFrom ParseHorizontalRelativeFrom(DW.HorizontalRelativePositionValues? value)
	{
		if (value is null)
		{
			return AnchorRelativeFrom.Unknown;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Page)
		{
			return AnchorRelativeFrom.Page;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Margin)
		{
			return AnchorRelativeFrom.Margin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Column)
		{
			return AnchorRelativeFrom.Column;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Character)
		{
			return AnchorRelativeFrom.Character;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.LeftMargin)
		{
			return AnchorRelativeFrom.LeftMargin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.RightMargin)
		{
			return AnchorRelativeFrom.RightMargin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.InsideMargin)
		{
			return AnchorRelativeFrom.InsideMargin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.OutsideMargin)
		{
			return AnchorRelativeFrom.OutsideMargin;
		}

		return AnchorRelativeFrom.Unknown;
	}

	private static AnchorRelativeFrom ParseVerticalRelativeFrom(DW.VerticalRelativePositionValues? value)
	{
		if (value is null)
		{
			return AnchorRelativeFrom.Unknown;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Page)
		{
			return AnchorRelativeFrom.Page;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Margin)
		{
			return AnchorRelativeFrom.Margin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Paragraph)
		{
			return AnchorRelativeFrom.Paragraph;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Line)
		{
			return AnchorRelativeFrom.Line;
		}

		if (value.Value == DW.VerticalRelativePositionValues.TopMargin)
		{
			return AnchorRelativeFrom.TopMargin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.BottomMargin)
		{
			return AnchorRelativeFrom.BottomMargin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.InsideMargin)
		{
			return AnchorRelativeFrom.InsideMargin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.OutsideMargin)
		{
			return AnchorRelativeFrom.OutsideMargin;
		}

		return AnchorRelativeFrom.Unknown;
	}

	private static AnchorAlignment ParseHorizontalAlignment(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return AnchorAlignment.None;
		}

		return value.Trim().ToLowerInvariant() switch
		{
			"left" => AnchorAlignment.Left,
			"center" => AnchorAlignment.Center,
			"right" => AnchorAlignment.Right,
			"inside" => AnchorAlignment.Inside,
			"outside" => AnchorAlignment.Outside,
			_ => AnchorAlignment.None
		};
	}

	private static AnchorAlignment ParseVerticalAlignment(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return AnchorAlignment.None;
		}

		return value.Trim().ToLowerInvariant() switch
		{
			"top" => AnchorAlignment.Top,
			"center" => AnchorAlignment.Center,
			"bottom" => AnchorAlignment.Bottom,
			"inside" => AnchorAlignment.Inside,
			"outside" => AnchorAlignment.Outside,
			_ => AnchorAlignment.None
		};
	}

	private static bool ParseOnOffValue(BooleanValue? value)
	{
		if (value is null)
		{
			return false;
		}

		return value.Value;
	}
}
