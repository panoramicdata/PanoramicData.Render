using DocumentFormat.OpenXml.Wordprocessing;
using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>
/// Lays out text frame content for shapes and text boxes.
/// </summary>
internal static class TextBoxLayoutEngine
{
	internal const float DefaultFontSizePoints = 12f;
	internal const float DefaultLineHeightTwips = 240f;

	/// <summary>
	/// Computes layout blocks for text box content using the existing paragraph and table layout engines.
	/// </summary>
	/// <param name="textFrame">The parsed text frame to lay out.</param>
	/// <param name="availableWidthTwips">The usable content width inside the text box, in twips.</param>
	/// <param name="fontFamily">The fallback font family used for paragraph measurement.</param>
	/// <param name="fontSizePoints">The fallback font size in points.</param>
	/// <returns>A tuple of the laid-out blocks and their total height in twips.</returns>
	public static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeightTwips) Layout(
		ShapeTextFrameInfo textFrame,
		float availableWidthTwips,
		string fontFamily = "Times New Roman",
		float fontSizePoints = DefaultFontSizePoints)
	{
		ArgumentNullException.ThrowIfNull(textFrame);

		if (availableWidthTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(availableWidthTwips));
		}

		if (fontSizePoints <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontSizePoints));
		}

		if (!textFrame.HasTextFrame)
		{
			return ([], 0f);
		}

		var contentBlocks = GetContentBlocks(textFrame);
		if (contentBlocks.Count == 0)
		{
			return ([], 0f);
		}

		var measurementEngine = new MeasurementEngine();
		var lineBreaker = new ParagraphLineBreaker(measurementEngine);
		var typeface = ResolveTypeface(fontFamily);
		var layoutBlocks = new List<LayoutBlock>(contentBlocks.Count);
		var totalHeight = 0f;

		foreach (var block in contentBlocks)
		{
			var layoutBlock = LayoutBlock(block, availableWidthTwips, fontSizePoints, typeface, lineBreaker);
			layoutBlocks.Add(layoutBlock);
			totalHeight += layoutBlock.HeightTwips;
		}

		return (layoutBlocks, totalHeight);
	}

	private static IReadOnlyList<DocumentBlock> GetContentBlocks(ShapeTextFrameInfo textFrame)
	{
		if (textFrame.Blocks.Count > 0)
		{
			return textFrame.Blocks;
		}

		if (string.IsNullOrWhiteSpace(textFrame.Text))
		{
			return [];
		}

		return textFrame.Text
			.Split(['\n'], StringSplitOptions.None)
			.Select(CreateSyntheticParagraphBlock)
			.ToArray();
	}

	private static LayoutBlock LayoutBlock(
		DocumentBlock block,
		float availableWidthTwips,
		float fontSizePoints,
		SKTypeface typeface,
		ParagraphLineBreaker lineBreaker)
	{
		return block switch
		{
			ParagraphBlock paragraphBlock => LayoutParagraph(paragraphBlock, availableWidthTwips, fontSizePoints, typeface, lineBreaker),
			TablePlaceholderBlock tableBlock => LayoutTable(tableBlock, availableWidthTwips),
			_ => new LayoutBlock(block, DefaultLineHeightTwips)
		};
	}

	private static LayoutBlock LayoutParagraph(
		ParagraphBlock paragraphBlock,
		float availableWidthTwips,
		float fontSizePoints,
		SKTypeface typeface,
		ParagraphLineBreaker lineBreaker)
	{
		var runs = RunElementParser.ParseParagraphRuns(paragraphBlock.SourceElement);
		var lines = runs.Count == 0
			? []
			: lineBreaker.ComputeLineBreaks(runs, typeface, fontSizePoints, availableWidthTwips);
		var lineCount = Math.Max(lines.Count, 1);
		var lineHeights = Enumerable.Repeat(DefaultLineHeightTwips, lineCount).ToArray();
		var height = ParagraphSpacing.None.ComputeParagraphHeight(lineCount, DefaultLineHeightTwips);

		return new LayoutBlock(
			paragraphBlock,
			height,
			LineHeights: lineHeights);
	}

	private static LayoutBlock LayoutTable(TablePlaceholderBlock tableBlock, float availableWidthTwips)
	{
		var parsedTable = TableParser.Parse(tableBlock.TableElement);
		var tableLayout = TableLayoutEngine.Layout(parsedTable, availableWidthTwips);
		return new LayoutBlock(tableBlock, tableLayout.TotalHeightTwips);
	}

	private static ParagraphBlock CreateSyntheticParagraphBlock(string line)
	{
		var paragraph = string.IsNullOrEmpty(line)
			? new Paragraph()
			: new Paragraph(new Run(new Text(line)));
		return DocumentBlockParser.CreateParagraphBlock(paragraph);
	}

	private static SKTypeface ResolveTypeface(string fontFamily)
	{
		var requestedFamily = string.IsNullOrWhiteSpace(fontFamily) ? "Times New Roman" : fontFamily;
		return SKTypeface.FromFamilyName(requestedFamily) ?? SKTypeface.Default;
	}
}