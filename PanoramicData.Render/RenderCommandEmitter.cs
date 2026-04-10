namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

/// <summary>
/// Emits rendering commands from paginated layout blocks.
/// </summary>
internal static class RenderCommandEmitter
{
	private const float DefaultTextBaselineOffsetTwips = 240f;
	private const float AverageGlyphWidthFactor = 10f;
	private static readonly RenderColor DefaultTextColor = new(0, 0, 0);

	/// <summary>
	/// Emits drawing commands for all pages.
	/// </summary>
	/// <param name="pages">The paginated layout pages.</param>
	/// <param name="target">The render target that receives drawing commands.</param>
	/// <param name="options">Optional render options.</param>
	public static void EmitDocument(IReadOnlyList<LayoutPage> pages, IRenderTarget target, RenderOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(pages);
		ArgumentNullException.ThrowIfNull(target);
		var renderTimestampUtc = DateTime.UtcNow;

		foreach (var page in pages)
		{
			EmitPage(page, target, options, pages.Count, renderTimestampUtc);
		}
	}

	/// <summary>
	/// Emits drawing commands for a single page.
	/// </summary>
	/// <param name="page">The page to emit.</param>
	/// <param name="target">The render target that receives drawing commands.</param>
	/// <param name="options">Optional render options.</param>
	/// <param name="totalPageCount">Optional total page count used by NUMPAGES fields.</param>
	/// <param name="renderTimestampUtc">Optional timestamp used by DATE/TIME fields.</param>
	public static void EmitPage(LayoutPage page, IRenderTarget target, RenderOptions? options = null, int? totalPageCount = null, DateTime? renderTimestampUtc = null)
	{
		ArgumentNullException.ThrowIfNull(page);
		ArgumentNullException.ThrowIfNull(target);

		var renderOptions = options ?? new RenderOptions();
		var fontFamily = string.IsNullOrWhiteSpace(renderOptions.FallbackFontFamily)
			? "Times New Roman"
			: renderOptions.FallbackFontFamily;
		var defaultFont = new RenderFont(fontFamily, 12f);
		var defaultBrush = new SolidRenderBrush(DefaultTextColor);
		var defaultStroke = new RenderStroke(DefaultTextColor, 8f);
		var effectiveTotalPageCount = totalPageCount ?? Math.Max(page.PageNumber, 1);
		var effectiveTimestampUtc = renderTimestampUtc ?? DateTime.UtcNow;
		var contentWidth = page.Section.PageWidth - page.Section.MarginLeft - page.Section.MarginRight;

		var yTwips = page.ContentTopTwips;
		foreach (var layoutBlock in page.Blocks)
		{
			switch (layoutBlock.Block)
			{
				case ParagraphBlock paragraphBlock:
				{
						var baselineOffset = MathF.Min(DefaultTextBaselineOffsetTwips, layoutBlock.HeightTwips);
						var baselineY = yTwips + baselineOffset;
						var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, fontFamily, page.PageNumber, effectiveTotalPageCount, effectiveTimestampUtc);
						var currentX = (float)page.Section.MarginLeft;
						foreach (var segment in segments)
					{
							target.DrawText(segment.Text, currentX, baselineY, segment.Font, defaultBrush);
							currentX += EstimateTextWidthTwips(segment.Text, segment.Font.SizePoints);
					}
					break;
				}
				case TablePlaceholderBlock:
				{
					var heightTwips = MathF.Max(layoutBlock.HeightTwips, 1f);
					target.DrawRect(new RenderRect(page.Section.MarginLeft, yTwips, contentWidth, heightTwips), null, defaultStroke);
					break;
				}
			}

			yTwips += layoutBlock.HeightTwips;
		}
	}

	private static IReadOnlyList<TextSegment> BuildTextSegments(Paragraph paragraph, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		var segments = new List<TextSegment>();
		var activeFields = new Stack<ActiveField>();
		foreach (var child in paragraph.ChildElements)
		{
			AppendSegmentsFromElement(child, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc);
		}

		if (segments.Count == 0)
		{
			var text = paragraph.InnerText;
			if (!string.IsNullOrWhiteSpace(text))
			{
				segments.Add(new TextSegment(text, defaultFont));
			}
		}

		return segments;
	}

	private static void AppendSegmentsFromElement(OpenXmlElement element, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, Stack<ActiveField> activeFields, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		switch (element)
		{
			case Run run:
				AppendSegmentsFromRun(run, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc);
				break;
			case SimpleField simpleField:
				AppendSegmentsFromSimpleField(simpleField, segments, defaultFont, defaultFamily, currentPageNumber, totalPageCount, renderTimestampUtc);
				break;
			default:
				foreach (var child in element.ChildElements)
				{
					AppendSegmentsFromElement(child, segments, defaultFont, defaultFamily, activeFields, currentPageNumber, totalPageCount, renderTimestampUtc);
				}
				break;
		}
	}

	private static void AppendSegmentsFromRun(Run run, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, Stack<ActiveField> activeFields, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		var runProperties = run.RunProperties;
		var font = ResolveRunFont(runProperties, defaultFont, defaultFamily);

		foreach (var fieldCode in run.Elements<FieldCode>())
		{
			if (activeFields.Count > 0)
			{
				activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
			}
		}

		foreach (var fieldChar in run.Elements<FieldChar>())
		{
			HandleFieldChar(fieldChar, activeFields);
		}

		var text = string.Concat(run.Elements<Text>().Select(t => t.Text));
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (activeFields.Count == 0)
		{
			AppendTextSegment(segments, text, font);
			return;
		}

		var activeField = activeFields.Peek();
		if (!activeField.IsResultSection)
		{
			return;
		}

		if (activeField.Kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			if (!activeField.HasRenderedComputedValue)
			{
				AppendTextSegment(segments, ComputeFieldValue(activeField.Kind, currentPageNumber, totalPageCount, renderTimestampUtc), font);
				activeField.HasRenderedComputedValue = true;
			}

			return;
		}

		AppendTextSegment(segments, text, font);
	}

	private static void AppendSegmentsFromSimpleField(SimpleField simpleField, List<TextSegment> segments, RenderFont defaultFont, string defaultFamily, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		var kind = ParseFieldKind(simpleField.Instruction?.Value);
		if (kind is FieldKind.Page or FieldKind.NumPages or FieldKind.Date or FieldKind.Time)
		{
			AppendTextSegment(segments, ComputeFieldValue(kind, currentPageNumber, totalPageCount, renderTimestampUtc), defaultFont);
			return;
		}

		var text = string.Concat(simpleField.Descendants<Text>().Select(t => t.Text));
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var firstRunProperties = simpleField.Descendants<Run>().Select(r => r.RunProperties).FirstOrDefault(r => r is not null);
		var font = ResolveRunFont(firstRunProperties, defaultFont, defaultFamily);
		AppendTextSegment(segments, text, font);
	}

	private static void HandleFieldChar(FieldChar fieldChar, Stack<ActiveField> activeFields)
	{
		if (fieldChar.FieldCharType is null)
		{
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Begin)
		{
			activeFields.Push(new ActiveField());
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
			activeField.Kind = ParseFieldKind(activeField.InstructionBuilder.ToString());
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
			defaultFont.SizePoints,
			IsOn(runProperties?.Bold),
			IsOn(runProperties?.Italic),
			IsUnderline(runProperties?.Underline),
			IsOn(runProperties?.Strike));
	}

	private static void AppendTextSegment(List<TextSegment> segments, string text, RenderFont font)
	{
		if (segments.Count > 0 && segments[^1].Font == font)
		{
			segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
		}
		else
		{
			segments.Add(new TextSegment(text, font));
		}
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

	private static string ComputeFieldValue(FieldKind fieldKind, int currentPageNumber, int totalPageCount, DateTime renderTimestampUtc)
	{
		return fieldKind switch
		{
			FieldKind.Page => currentPageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.NumPages => totalPageCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.Date => renderTimestampUtc.ToString("d", System.Globalization.CultureInfo.InvariantCulture),
			FieldKind.Time => renderTimestampUtc.ToString("T", System.Globalization.CultureInfo.InvariantCulture),
			_ => string.Empty
		};
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

	private sealed class ActiveField
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldKind Kind { get; set; } = FieldKind.Other;

		public bool HasRenderedComputedValue { get; set; }
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

	private readonly record struct TextSegment(string Text, RenderFont Font);
}
