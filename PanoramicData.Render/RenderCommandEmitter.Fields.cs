namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using SkiaSharp;
using System.Globalization;
using System.Text;

internal static partial class RenderCommandEmitter
{
	private static RenderBrush ResolveRunBrush(RunProperties? runProperties)
	{
		return TryParseRenderColor(runProperties?.Color?.Val?.Value, out var color)
			? new SolidRenderBrush(color)
			: new SolidRenderBrush(DefaultTextColor);
	}

	private static bool TryParseRenderColor(string? colorValue, out RenderColor color)
	{
		if (string.IsNullOrWhiteSpace(colorValue))
		{
			color = default;
			return false;
		}

		if (string.Equals(colorValue, "auto", StringComparison.OrdinalIgnoreCase))
		{
			color = DefaultTextColor;
			return true;
		}

		if (colorValue.Length == 6
			&& byte.TryParse(colorValue.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
			&& byte.TryParse(colorValue.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
			&& byte.TryParse(colorValue.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
		{
			color = new RenderColor(r, g, b);
			return true;
		}

		color = colorValue.ToUpperInvariant() switch
		{
			"RED" => new RenderColor(255, 0, 0),
			"BLUE" => new RenderColor(0, 0, 255),
			"GREEN" => new RenderColor(0, 128, 0),
			"BLACK" => new RenderColor(0, 0, 0),
			"WHITE" => new RenderColor(255, 255, 255),
			_ => default,
		};

		return color != default;
	}

	private static string? ExtractHyperlinkUri(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return null;
		}

		var index = instruction.IndexOf("HYPERLINK", StringComparison.OrdinalIgnoreCase);
		if (index < 0)
		{
			return null;
		}

		var remaining = instruction[(index + "HYPERLINK".Length)..].Trim();
		if (remaining.Length == 0)
		{
			return null;
		}

		if (remaining[0] == '"')
		{
			var endQuote = remaining.IndexOf('"', 1);
			if (endQuote > 1)
			{
				return remaining[1..endQuote];
			}

			return null;
		}

		var token = remaining.Split([' ', '\\', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];
		return token.Length == 0 ? null : token;
	}

	private static FieldKind ParseFieldKind(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return FieldKind.Other;
		}

		var trimmed = instruction.Trim();
		var firstToken = trimmed.Split([' ', '\\', '"', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();
		return firstToken switch
		{
			"PAGE" => FieldKind.Page,
			"NUMPAGES" => FieldKind.NumPages,
			"DATE" => FieldKind.Date,
			"TIME" => FieldKind.Time,
			"TOC" => FieldKind.Toc,
			"HYPERLINK" => FieldKind.Hyperlink,
			"REF" => FieldKind.Ref,
			"PAGEREF" => FieldKind.PageRef,
			"IF" => FieldKind.If,
			"MERGEFIELD" => FieldKind.MergeField,
			_ => FieldKind.Other
		};
	}

	private static string ComputeFieldValue(FieldKind fieldKind, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc, string? instruction = null)
	{
		return fieldKind switch
		{
			FieldKind.Page => currentPageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.NumPages => totalPageCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.Date => FormatDateTimeField(renderTimestampUtc, instruction, "d"),
			FieldKind.Time => FormatDateTimeField(renderTimestampUtc, instruction, "T"),
			_ => string.Empty
		};
	}

	/// <summary>
	/// Formats a DATE or TIME field using the OOXML \@ picture switch when present.
	/// Falls back to <paramref name="fallbackFormat"/> when no switch is found.
	/// </summary>
	private static string FormatDateTimeField(DateTime dt, string? instruction, string fallbackFormat)
	{
		var picture = TryExtractDatePictureSwitch(instruction);
		if (picture is null)
		{
			return dt.ToString(fallbackFormat, CultureInfo.InvariantCulture);
		}

		var dotNetFormat = ConvertOoxmlDatePictureToDotNet(picture);
		return dt.ToString(dotNetFormat, CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Extracts the quoted format string from an OOXML \@ date-picture switch,
	/// e.g. <c>DATE \@ "yyyy"</c> → <c>yyyy</c>.
	/// Returns <see langword="null"/> when no switch is present.
	/// </summary>
	private static string? TryExtractDatePictureSwitch(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return null;
		}

		// Match \@ "<picture>" (with optional extra whitespace)
		var idx = instruction.IndexOf(@"\@", StringComparison.Ordinal);
		if (idx < 0)
		{
			return null;
		}

		var after = instruction.AsSpan(idx + 2).TrimStart();
		if (after.IsEmpty || after[0] != '"')
		{
			return null;
		}

		var closeQuote = after[1..].IndexOf('"');
		if (closeQuote < 0)
		{
			return null;
		}

		return after.Slice(1, closeQuote).ToString();
	}

	/// <summary>
	/// Converts an OOXML date-picture string to a .NET <see cref="DateTime.ToString(string)"/> format string.
	/// OOXML uses the same tokens as Word's field formatting: d/dd/ddd/dddd, M/MM/MMM/MMMM, yy/yyyy, h/hh/H/HH, m/mm, s/ss, AM/PM.
	/// </summary>
	private static string ConvertOoxmlDatePictureToDotNet(string picture)
	{
		// OOXML picture codes map directly to .NET format codes with minor differences.
		// The main difference: OOXML uses single 'y' for 2-digit year and 'yyyy' for 4-digit.
		// .NET uses 'yy' for 2-digit year; a single 'y' means 1-or-2-digit.
		// We normalise lone 'y'→'yy' so that the .NET result matches Word's output.
		if (picture == "y")
		{
			return "yy";
		}

		// All other OOXML date picture tokens (d, dd, ddd, dddd, M, MM, MMM, MMMM, yy, yyyy,
		// h, hh, H, HH, m, mm, s, ss) are identical in .NET.
		return picture;
	}

	private static bool IsOn(OnOffType? property)
	{
		if (property is null)
		{
			return false;
		}

		return property.Val is null || property.Val.Value;
	}

	private static bool IsUnderline(Underline? underline)
	{
		if (underline is null)
		{
			return false;
		}

		return underline.Val is null || underline.Val.Value != UnderlineValues.None;
	}

	private static float EstimateTextWidthTwips(string text, float sizePoints)
	{
		return text.Length * sizePoints * AverageGlyphWidthFactor;
	}

	private static float ComputeTotalSegmentWidth(IReadOnlyList<TextSegment> segments)
	{
		var total = 0f;
		for (var i = 0; i < segments.Count; i++)
		{
			if (!segments[i].IsTab)
			{
				total += EstimateTextWidthTwips(segments[i].Text, segments[i].Font.SizePoints);
			}
		}

		return total;
	}

	private static float EstimateTextHeightTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints) * 1.2f, 1f);
	}
	private static float EstimateHighlightTopOffsetTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints * BaselineAscentFactor), 1f);
	}

	private static float EstimateHighlightHeightTwips(float sizePoints)
	{
		return MathF.Max(TwipConverter.PointsToTwips(sizePoints), 1f);
	}

	private static string GetTextAfterTab(IReadOnlyList<TextSegment> segments, int tabIndex)
	{
		var builder = new StringBuilder();
		for (var i = tabIndex + 1; i < segments.Count; i++)
		{
			if (segments[i].IsTab)
			{
				break;
			}

			builder.Append(segments[i].Text);
		}

		return builder.ToString();
	}

	private enum FieldKind
	{
		Other,
		Page,
		NumPages,
		Date,
		Time,
		Toc,
		Hyperlink,
		Ref,
		PageRef,
		If,
		MergeField
	}

	private readonly record struct TextSegment(string Text, RenderFont Font, RenderBrush Brush, string? HyperlinkUri, RenderColor? HighlightFillColor = null, bool IsTab = false, bool IsRtl = false, bool IsPtabRightMargin = false);
	private readonly record struct WrappedToken(string Text, float WidthTwips, RenderFont Font, RenderBrush Brush, string? HyperlinkUri, RenderColor? HighlightFillColor, bool IsWhitespace);
	private readonly record struct WrappedTextSegment(string Text, float XOffset, float WidthTwips, RenderFont Font, RenderBrush Brush, string? HyperlinkUri, RenderColor? HighlightFillColor);

	private readonly record struct TextDrawing(string Text, RenderFont Font, RenderBrush Brush, RenderColor? HighlightFillColor, string? HyperlinkUri);

	private sealed record TableEmitFrame(TableLayoutResult Layout, ResolvedGridCell?[,] Grid, float X, float Y);

	private readonly record struct RunStyle(RenderFont Font, RenderBrush Brush, RenderColor? HighlightFillColor, string? HyperlinkUri, bool IsRtl);

	private readonly record struct ImageLayoutItem(bool IsTab, bool IsImage, bool IsPtabRightMargin, float Width, Drawing? Drawing);

	private sealed record WrapPlan(IReadOnlyList<WrappedToken> Tokens, IReadOnlyList<KnuthPlassItem> Items, IReadOnlyList<int> ItemTokenIndexes);

	private sealed record BlockEmitContext(
		IRenderTarget Target,
		RenderOptions RenderOptions,
		RenderFont DefaultFont,
		string FontFamily,
		RenderBrush DefaultBrush,
		RenderStroke DefaultStroke,
		int CurrentPageNumber,
		int TotalPageCount,
		DateTime RenderTimestampUtc,
		ListNumberingState ListState,
		SectionInfo? Section,
		IReadOnlyDictionary<string, ImageData>? Images,
		Styles? Styles);

	private sealed record SegmentBuildContext(
		List<TextSegment> Segments,
		RenderFont DefaultFont,
		string DefaultFamily,
		Stack<ActiveField> ActiveFields,
		int CurrentPageNumber,
		int TotalPageCount,
		DateTime RenderTimestampUtc,
		OpenXmlPart? Part);
	private readonly record struct WrappedLine(IReadOnlyList<WrappedTextSegment> Segments);
}
