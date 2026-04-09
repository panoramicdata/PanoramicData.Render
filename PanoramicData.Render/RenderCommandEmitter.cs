namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Emits rendering commands from paginated layout blocks.
/// </summary>
internal static class RenderCommandEmitter
{
	private const float DefaultTextBaselineOffsetTwips = 240f;
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
					var text = paragraphBlock.SourceElement.InnerText;
					if (!string.IsNullOrWhiteSpace(text))
					{
						var baselineOffset = MathF.Min(DefaultTextBaselineOffsetTwips, layoutBlock.HeightTwips);
						target.DrawText(text, page.Section.MarginLeft, yTwips + baselineOffset, defaultFont, defaultBrush);
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
}
