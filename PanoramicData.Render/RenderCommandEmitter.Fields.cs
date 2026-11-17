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
}
