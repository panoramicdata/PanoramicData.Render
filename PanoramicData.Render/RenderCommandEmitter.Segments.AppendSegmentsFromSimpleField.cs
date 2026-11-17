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
		return new RenderFont(
			ResolveFontFamily(runProperties, defaultFamily),
			ResolveFontSizePoints(runProperties, defaultFont.SizePoints),
			IsOn(runProperties?.Bold),
			IsOn(runProperties?.Italic),
			IsUnderline(runProperties?.Underline),
			IsOn(runProperties?.Strike));
	}

	private static string ResolveFontFamily(RunProperties? runProperties, string defaultFamily)
	{
		var fontFamily = runProperties?.RunFonts?.Ascii?.Value;
		return string.IsNullOrWhiteSpace(fontFamily) ? defaultFamily : fontFamily;
	}
}
