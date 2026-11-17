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
}
