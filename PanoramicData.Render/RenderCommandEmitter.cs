namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

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

		foreach (var page in pages)
		{
			EmitPage(page, target, options);
		}
	}

	/// <summary>
	/// Emits drawing commands for a single page.
	/// </summary>
	/// <param name="page">The page to emit.</param>
	/// <param name="target">The render target that receives drawing commands.</param>
	/// <param name="options">Optional render options.</param>
	public static void EmitPage(LayoutPage page, IRenderTarget target, RenderOptions? options = null)
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
						var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, fontFamily);
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

	private static IReadOnlyList<TextSegment> BuildTextSegments(Paragraph paragraph, RenderFont defaultFont, string defaultFamily)
	{
		var segments = new List<TextSegment>();
		foreach (var run in paragraph.Elements<Run>())
		{
			var text = string.Concat(run.Elements<Text>().Select(t => t.Text));
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}

			var runProperties = run.RunProperties;
			var fontFamily = runProperties?.RunFonts?.Ascii?.Value;
			var font = new RenderFont(
				string.IsNullOrWhiteSpace(fontFamily) ? defaultFamily : fontFamily,
				defaultFont.SizePoints,
				IsOn(runProperties?.Bold),
				IsOn(runProperties?.Italic),
				IsUnderline(runProperties?.Underline),
				IsOn(runProperties?.Strike));

			if (segments.Count > 0 && segments[^1].Font == font)
			{
				segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
			}
			else
			{
				segments.Add(new TextSegment(text, font));
			}
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

	private readonly record struct TextSegment(string Text, RenderFont Font);
}
