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
	private static NumberingLevelStyle ResolveListStyle(RenderOptions options, int numberingId, int numberingLevel)
	{
		var canonicalId = options.NumberingIdNormalization.TryGetValue(numberingId, out var cid) ? cid : numberingId;
		var styleKey = CreateNumberingStyleKey(canonicalId, numberingLevel);
		if (options.NumberingStyles.TryGetValue(styleKey, out var configuredStyle))
		{
			return configuredStyle;
		}

		return new NumberingLevelStyle
		{
			LevelIndex = numberingLevel,
			Start = 1,
			NumberFormat = "decimal",
			LevelText = BuildDefaultLevelPattern(numberingLevel)
		};
	}

	private static string CreateNumberingStyleKey(int numberingId, int numberingLevel)
	{
		return $"{numberingId}:{numberingLevel}";
	}

	private static string BuildDefaultLevelPattern(int numberingLevel)
	{
		if (numberingLevel < 0)
		{
			return "%1.";
		}

		var parts = new List<string>(numberingLevel + 1);
		for (var index = 1; index <= numberingLevel + 1; index++)
		{
			parts.Add($"%{index}");
		}

		return string.Join(".", parts) + ".";
	}

	private sealed class ActiveField
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldKind Kind { get; set; } = FieldKind.Other;

		public string? HyperlinkUri { get; set; }

		public bool HasRenderedComputedValue { get; set; }

		/// <summary>
		/// Font from the field begin run, used to format computed field values.
		/// </summary>
		public RenderFont? BeginFont { get; set; }

		/// <summary>
		/// Brush from the field begin run, used to format computed field values.
		/// </summary>
		public RenderBrush? BeginBrush { get; set; }

		/// <summary>
		/// Highlight fill from the field begin run, used to format computed field values.
		/// </summary>
		public RenderColor? BeginHighlightFillColor { get; set; }
	}

	private static IReadOnlyList<TextSegment> BuildTextSegments(Paragraph paragraph, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc, OpenXmlPart? part = null)
	{
		var segments = new List<TextSegment>();
		var context = new SegmentBuildContext(segments, defaultFont, defaultFamily, new Stack<ActiveField>(), currentPageNumber, totalPageCount, renderTimestampUtc, part);
		foreach (var child in paragraph.ChildElements)
		{
			AppendSegmentsFromElement(child, context);
		}

		if (segments.Count == 0)
		{
			var text = paragraph.InnerText;
			if (!string.IsNullOrWhiteSpace(text))
			{
				segments.Add(new TextSegment(text, defaultFont, new SolidRenderBrush(DefaultTextColor), null, null));
			}
		}

		return segments;
	}

	private static void AppendSegmentsFromElement(OpenXmlElement element, SegmentBuildContext context, string? hyperlinkUri = null)
	{
		switch (element)
		{
			case Run run:
				AppendSegmentsFromRun(run, context, hyperlinkUri);
				break;
			case SimpleField simpleField:
				AppendSegmentsFromSimpleField(simpleField, context.Segments, context.DefaultFont, context.DefaultFamily, context.CurrentPageNumber, context.TotalPageCount, context.RenderTimestampUtc);
				break;
			case Hyperlink hyperlink:
				var resolvedUri = RunElementParser.ResolveHyperlinkUri(hyperlink, context.Part);
				foreach (var child in hyperlink.ChildElements)
				{
					AppendSegmentsFromElement(child, context, resolvedUri);
				}

				break;
			default:
				foreach (var child in element.ChildElements)
				{
					AppendSegmentsFromElement(child, context, hyperlinkUri);
				}
				break;
		}
	}

	private static void AppendSegmentsFromRun(Run run, SegmentBuildContext context, string? hyperlinkUri = null)
	{
		var segments = context.Segments;
		var activeFields = context.ActiveFields;
		var runProperties = run.RunProperties;
		var font = ResolveRunFont(runProperties, context.DefaultFont, context.DefaultFamily);
		var brush = ResolveRunBrush(runProperties);
		var highlightFillColor = ResolveRunHighlightFillColor(runProperties);
		var isRtl = IsOn(runProperties?.RightToLeftText);
		var style = new RunStyle(font, brush, highlightFillColor, hyperlinkUri, isRtl);

		foreach (var fieldCode in run.Elements<FieldCode>().Where(_ => activeFields.Count > 0))
		{
			activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
		}

		foreach (var fieldChar in run.Elements<FieldChar>())
		{
			HandleFieldChar(fieldChar, activeFields, font, brush, highlightFillColor);
		}

		// Process run children in document order to capture tabs interspersed with text
		var textBuilder = new StringBuilder();
		foreach (var child in run.ChildElements)
		{
			if (child is Text t)
			{
				textBuilder.Append(t.Text);
			}
			else if (child is TabChar)
			{
				FlushRunText(context, textBuilder, style);
				segments.Add(new TextSegment("\t", font, brush, null, highlightFillColor, IsTab: true));
			}
			else if (child is PositionalTab pTab)
			{
				FlushRunText(context, textBuilder, style);

				// w:ptab with relativeTo="margin" and alignment="right" snaps to the right margin.
				var isPtabRight = pTab.Alignment?.Value == AbsolutePositionTabAlignmentValues.Right;
				segments.Add(new TextSegment("\t", font, brush, null, highlightFillColor, IsTab: true, IsRtl: isRtl, IsPtabRightMargin: isPtabRight));
			}
		}

		FlushRunText(context, textBuilder, style);
	}

	private static void FlushRunText(SegmentBuildContext context, StringBuilder textBuilder, RunStyle style)
	{
		if (textBuilder.Length == 0)
		{
			return;
		}

		RouteTextToSegment(context, textBuilder.ToString(), style);
		textBuilder.Clear();
	}

	private static void RouteTextToSegment(SegmentBuildContext context, string text, RunStyle style)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		var segments = context.Segments;
		if (context.ActiveFields.Count == 0)
		{
			AppendTextSegment(segments, text, style.Font, style.Brush, style.HighlightFillColor, style.HyperlinkUri, style.IsRtl);
			return;
		}

		var activeField = context.ActiveFields.Peek();
		if (!activeField.IsResultSection)
		{
			return;
		}

		if (activeField.Kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			AppendComputedFieldValue(context, activeField, style);
			return;
		}

		AppendTextSegment(segments, text, style.Font, style.Brush, style.HighlightFillColor, activeField.HyperlinkUri, style.IsRtl);
	}

	private static void AppendComputedFieldValue(SegmentBuildContext context, ActiveField activeField, RunStyle style)
	{
		if (activeField.HasRenderedComputedValue)
		{
			return;
		}

		var fieldFont = activeField.BeginFont ?? style.Font;
		var fieldBrush = activeField.BeginBrush ?? style.Brush;
		var fieldHighlightFillColor = activeField.BeginHighlightFillColor ?? style.HighlightFillColor;
		var value = ComputeFieldValue(activeField.Kind, context.CurrentPageNumber, context.TotalPageCount, context.RenderTimestampUtc, activeField.InstructionBuilder.ToString());
		AppendTextSegment(context.Segments, value, fieldFont, fieldBrush, fieldHighlightFillColor, null, style.IsRtl);
		activeField.HasRenderedComputedValue = true;
	}
	private static void AppendSegmentsFromSimpleField(SimpleField simpleField, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		var instructionText = simpleField.Instruction?.Value;
		var hyperlinkUri = ExtractHyperlinkUri(instructionText);
		var kind = ParseFieldKind(simpleField.Instruction?.Value);
		var firstRunProperties = simpleField.Descendants<Run>().Select(r => r.RunProperties).FirstOrDefault(r => r is not null);
		var brush = ResolveRunBrush(firstRunProperties);
		var highlightFillColor = ResolveRunHighlightFillColor(firstRunProperties);
		if (kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			AppendTextSegment(segments, ComputeFieldValue(kind, currentPageNumber, totalPageCount, renderTimestampUtc, instructionText), defaultFont, brush, highlightFillColor, null);
			return;
		}

		var text = string.Concat(simpleField.Descendants<Text>().Select(t => t.Text));
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var font = ResolveRunFont(firstRunProperties, defaultFont, defaultFamily);
		AppendTextSegment(segments, text, font, brush, highlightFillColor, hyperlinkUri);
	}

	private static void HandleFieldChar(FieldChar fieldChar, Stack<ActiveField> activeFields, RenderFont? beginFont = null, RenderBrush? beginBrush = null, RenderColor? beginHighlightFillColor = null)
	{
		if (fieldChar.FieldCharType is null)
		{
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Begin)
		{
			activeFields.Push(new ActiveField { BeginFont = beginFont, BeginBrush = beginBrush, BeginHighlightFillColor = beginHighlightFillColor });
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Separate)
		{
			if (activeFields.Count == 0)
			{
				return;
			}

			var activeField = activeFields.Peek();
			activeField.IsResultSection = true;
			var instructionText = activeField.InstructionBuilder.ToString();
			activeField.Kind = ParseFieldKind(instructionText);
			activeField.HyperlinkUri = activeField.Kind == FieldKind.Hyperlink ? ExtractHyperlinkUri(instructionText) : null;
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.End && activeFields.Count > 0)
		{
			activeFields.Pop();
		}
	}

	private static RenderFont ResolveRunFont(RunProperties? runProperties, RenderFont defaultFont, string defaultFamily)
	{
		var fontFamily = runProperties?.RunFonts?.Ascii?.Value;
		return new RenderFont(
			string.IsNullOrWhiteSpace(fontFamily) ? defaultFamily : fontFamily,
			ResolveFontSizePoints(runProperties, defaultFont.SizePoints),
			IsOn(runProperties?.Bold),
			IsOn(runProperties?.Italic),
			IsUnderline(runProperties?.Underline),
			IsOn(runProperties?.Strike));
	}

	private static float ResolveFontSizePoints(RunProperties? runProperties, float defaultSizePoints)
		=> int.TryParse(runProperties?.FontSize?.Val?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeHalfPoints)
			? sizeHalfPoints / 2f
			: defaultSizePoints;
	private static void AppendTextSegment(List<TextSegment> segments, string text, RenderFont font, RenderBrush brush, RenderColor? highlightFillColor, string? hyperlinkUri, bool isRtl = false)
	{
		if (segments.Count > 0
			&& !segments[^1].IsTab
			&& segments[^1].IsRtl == isRtl
			&& segments[^1].Font == font
			&& segments[^1].Brush == brush
			&& segments[^1].HighlightFillColor == highlightFillColor
			&& string.Equals(segments[^1].HyperlinkUri, hyperlinkUri, StringComparison.Ordinal))
		{
			segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
		}
		else
		{
			segments.Add(new TextSegment(text, font, brush, hyperlinkUri, highlightFillColor, IsRtl: isRtl));
		}
	}

	private static readonly (HighlightColorValues Value, HighlightColor Color)[] HighlightColorTable =
	[
		(HighlightColorValues.Black, HighlightColor.Black),
		(HighlightColorValues.Blue, HighlightColor.Blue),
		(HighlightColorValues.Cyan, HighlightColor.Cyan),
		(HighlightColorValues.DarkBlue, HighlightColor.DarkBlue),
		(HighlightColorValues.DarkCyan, HighlightColor.DarkCyan),
		(HighlightColorValues.DarkGray, HighlightColor.DarkGray),
		(HighlightColorValues.DarkGreen, HighlightColor.DarkGreen),
		(HighlightColorValues.DarkMagenta, HighlightColor.DarkMagenta),
		(HighlightColorValues.DarkRed, HighlightColor.DarkRed),
		(HighlightColorValues.DarkYellow, HighlightColor.DarkYellow),
		(HighlightColorValues.Green, HighlightColor.Green),
		(HighlightColorValues.LightGray, HighlightColor.LightGray),
		(HighlightColorValues.Magenta, HighlightColor.Magenta),
		(HighlightColorValues.Red, HighlightColor.Red),
		(HighlightColorValues.White, HighlightColor.White),
		(HighlightColorValues.Yellow, HighlightColor.Yellow),
	];

	private static RenderColor? ResolveRunHighlightFillColor(RunProperties? runProperties)
	{
		var highlightColor = ResolveHighlightColor(runProperties?.Highlight?.Val?.Value);
		if (highlightColor == HighlightColor.None)
		{
			return null;
		}

		var highlightHex = HighlightColorMap.ToHexRgb(highlightColor);
		return TryParseRenderColor(highlightHex, out var highlightFillColor) ? highlightFillColor : null;
	}

	private static HighlightColor ResolveHighlightColor(HighlightColorValues? highlightValue)
	{
		foreach (var (value, color) in HighlightColorTable)
		{
			if (highlightValue == value)
			{
				return color;
			}
		}

		return HighlightColor.None;
	}
}
