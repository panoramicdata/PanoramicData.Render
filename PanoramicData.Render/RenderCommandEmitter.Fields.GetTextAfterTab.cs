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
