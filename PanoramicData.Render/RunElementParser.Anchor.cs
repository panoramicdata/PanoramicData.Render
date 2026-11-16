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

		var placement = new AnchorPlacementInfo
		{
			BehindDocument = ParseOnOffValue(anchor.BehindDoc),
			WrapStyle = ParseWrapStyle(anchor),
		};

		return WithDistances(WithVerticalPosition(WithHorizontalPosition(placement, anchor.GetFirstChild<DW.HorizontalPosition>()), anchor.GetFirstChild<DW.VerticalPosition>()), anchor);
	}

	private static AnchorPlacementInfo WithHorizontalPosition(AnchorPlacementInfo placement, DW.HorizontalPosition? position)
		=> placement with
		{
			HorizontalRelativeFrom = ParseHorizontalRelativeFrom(position?.RelativeFrom?.Value),
			HorizontalOffsetEmu = ParseOffset(position?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			HorizontalAlignment = ParseHorizontalAlignment(position?.GetFirstChild<DW.HorizontalAlignment>()?.InnerText),
		};

	private static AnchorPlacementInfo WithVerticalPosition(AnchorPlacementInfo placement, DW.VerticalPosition? position)
		=> placement with
		{
			VerticalRelativeFrom = ParseVerticalRelativeFrom(position?.RelativeFrom?.Value),
			VerticalOffsetEmu = ParseOffset(position?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			VerticalAlignment = ParseVerticalAlignment(position?.GetFirstChild<DW.VerticalAlignment>()?.InnerText),
		};

	private static AnchorPlacementInfo WithDistances(AnchorPlacementInfo placement, DW.Anchor anchor)
		=> placement with
		{
			DistanceTopEmu = ToEmu(anchor.DistanceFromTop),
			DistanceBottomEmu = ToEmu(anchor.DistanceFromBottom),
			DistanceLeftEmu = ToEmu(anchor.DistanceFromLeft),
			DistanceRightEmu = ToEmu(anchor.DistanceFromRight),
		};
	private static uint ToEmu(UInt32Value? value) => value?.Value ?? 0U;

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

	private static readonly (DW.HorizontalRelativePositionValues Value, AnchorRelativeFrom From)[] HorizontalRelativeFromMap =
	[
		(DW.HorizontalRelativePositionValues.Page, AnchorRelativeFrom.Page),
		(DW.HorizontalRelativePositionValues.Margin, AnchorRelativeFrom.Margin),
		(DW.HorizontalRelativePositionValues.Column, AnchorRelativeFrom.Column),
		(DW.HorizontalRelativePositionValues.Character, AnchorRelativeFrom.Character),
		(DW.HorizontalRelativePositionValues.LeftMargin, AnchorRelativeFrom.LeftMargin),
		(DW.HorizontalRelativePositionValues.RightMargin, AnchorRelativeFrom.RightMargin),
		(DW.HorizontalRelativePositionValues.InsideMargin, AnchorRelativeFrom.InsideMargin),
		(DW.HorizontalRelativePositionValues.OutsideMargin, AnchorRelativeFrom.OutsideMargin),
	];

	private static readonly (DW.VerticalRelativePositionValues Value, AnchorRelativeFrom From)[] VerticalRelativeFromMap =
	[
		(DW.VerticalRelativePositionValues.Page, AnchorRelativeFrom.Page),
		(DW.VerticalRelativePositionValues.Margin, AnchorRelativeFrom.Margin),
		(DW.VerticalRelativePositionValues.Paragraph, AnchorRelativeFrom.Paragraph),
		(DW.VerticalRelativePositionValues.Line, AnchorRelativeFrom.Line),
		(DW.VerticalRelativePositionValues.TopMargin, AnchorRelativeFrom.TopMargin),
		(DW.VerticalRelativePositionValues.BottomMargin, AnchorRelativeFrom.BottomMargin),
		(DW.VerticalRelativePositionValues.InsideMargin, AnchorRelativeFrom.InsideMargin),
		(DW.VerticalRelativePositionValues.OutsideMargin, AnchorRelativeFrom.OutsideMargin),
	];

	private static AnchorRelativeFrom ParseHorizontalRelativeFrom(DW.HorizontalRelativePositionValues? value)
	{
		foreach (var (candidate, from) in HorizontalRelativeFromMap)
		{
			if (value == candidate)
			{
				return from;
			}
		}

		return AnchorRelativeFrom.Unknown;
	}

	private static AnchorRelativeFrom ParseVerticalRelativeFrom(DW.VerticalRelativePositionValues? value)
	{
		foreach (var (candidate, from) in VerticalRelativeFromMap)
		{
			if (value == candidate)
			{
				return from;
			}
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
