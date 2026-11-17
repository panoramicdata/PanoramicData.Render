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
}
