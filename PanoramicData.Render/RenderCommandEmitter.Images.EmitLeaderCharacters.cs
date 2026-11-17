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
	private static void EmitLeaderCharacters(TabStopLeader leader, float fromX, float toX, float baselineY, RenderFont font, RenderBrush brush, IRenderTarget target)
	{
		if (leader == TabStopLeader.None || toX <= fromX)
		{
			return;
		}

		var leaderChar = leader switch
		{
			TabStopLeader.Dot => ".",
			TabStopLeader.Hyphen => "-",
			TabStopLeader.Underscore => "_",
			TabStopLeader.Heavy => "_",
			TabStopLeader.MiddleDot => "\u00B7",
			_ => null
		};

		if (leaderChar is null)
		{
			return;
		}

		var charWidth = EstimateTextWidthTwips(leaderChar, font.SizePoints);
		if (charWidth <= 0f)
		{
			return;
		}

		// Add small padding at start and end to avoid touching adjacent text
		var leaderFont = leader == TabStopLeader.Heavy ? font with { IsBold = true } : font;
		var x = fromX + charWidth;
		while (x + charWidth <= toX)
		{
			target.DrawText(leaderChar, x, baselineY, leaderFont, brush);
			x += charWidth;
		}
	}

	private static void EmitHeaderFooterBlocks(IReadOnlyList<LayoutBlock>? blocks, float topYTwips, LayoutPage page, BlockEmitContext context)
	{
		if (blocks is null or { Count: 0 })
		{
			return;
		}

		var currentY = topYTwips;
		foreach (var layoutBlock in blocks)
		{
			var maxInlineImageHeight = 0f;
			if (layoutBlock.Block is ParagraphBlock paragraphBlock)
			{
				maxInlineImageHeight = GetMaxInlineImageHeightTwips(paragraphBlock.SourceElement);
				EmitHeaderFooterParagraph(layoutBlock, paragraphBlock, currentY, maxInlineImageHeight, page, context);
			}

			currentY += MathF.Max(layoutBlock.HeightTwips, maxInlineImageHeight);
		}
	}

	private static void EmitHeaderFooterParagraph(LayoutBlock layoutBlock, ParagraphBlock paragraphBlock, float currentY, float maxInlineImageHeight, LayoutPage page, BlockEmitContext context)
	{
		var defaultFont = context.DefaultFont;
		var contentWidth = page.Section.PageWidth - page.Section.MarginLeft - page.Section.MarginRight;
		var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, context.FontFamily, page.PageNumber, context.TotalPageCount, context.RenderTimestampUtc);
		var hfFontSize = segments.Count > 0 ? segments[0].Font.SizePoints : defaultFont.SizePoints;
		var lineHeightTwips = MathF.Max(layoutBlock.HeightTwips, maxInlineImageHeight);
		var fontHeightTwips = TwipConverter.PointsToTwips(hfFontSize);
		var fontDescentTwips = fontHeightTwips * 0.2f;
		var baselineOffset = MathF.Max(fontHeightTwips, lineHeightTwips - fontDescentTwips);
		var baselineY = currentY + baselineOffset;
		var hfPlacement = new LayoutBlockPlacement(layoutBlock, (float)page.Section.MarginLeft, currentY, contentWidth, 0);

		EmitInlineSegments(segments, paragraphBlock, hfPlacement, baselineY, (float)page.Section.MarginLeft, context.Target, includeHyperlinks: false);

		if (paragraphBlock.Borders.HasAnyVisibleBorder)
		{
			EmitParagraphBorders(paragraphBlock.Borders, hfPlacement.XTwips, hfPlacement.YTwips, lineHeightTwips, hfPlacement.ContentWidthTwips, context.Target);
		}

		EmitParagraphImagesIfAny(paragraphBlock, hfPlacement, context);
	}
	private static float GetMaxInlineImageHeightTwips(Paragraph paragraph)
	{
		ArgumentNullException.ThrowIfNull(paragraph);

		var maxHeight = 0f;
		foreach (var drawing in paragraph.Descendants<Drawing>())
		{
			var inline = drawing.GetFirstChild<DW.Inline>();
			if (inline is null)
			{
				continue;
			}

			var heightTwips = TwipConverter.EmusToTwips(inline.Extent?.Cy ?? 0);
			if (heightTwips > maxHeight)
			{
				maxHeight = heightTwips;
			}
		}

		return maxHeight;
	}
}
