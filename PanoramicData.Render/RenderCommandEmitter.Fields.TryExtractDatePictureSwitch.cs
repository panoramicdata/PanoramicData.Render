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
}
