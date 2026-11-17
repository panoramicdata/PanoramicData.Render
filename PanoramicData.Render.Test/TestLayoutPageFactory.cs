namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

internal static class TestLayoutPageFactory
{
	public static LayoutPage Create(IReadOnlyList<LayoutBlock> blocks, int pageNumber = 1, WatermarkInfo? watermark = null, int pageWidth = 12240, int pageHeight = 15840, SectionInfo? section = null)
		=> new()
		{
			Section = section ?? new SectionInfo { PageWidth = pageWidth, PageHeight = pageHeight, MarginLeft = 720, MarginRight = 720 },
			PageNumber = pageNumber,
			ContentTopTwips = 1000,
			Blocks = blocks,
			Watermark = watermark
		};

	public static LayoutPage CreateTextPage(int pageNumber, string text, int pageWidth = 12240, int pageHeight = 15840)
		=> Create(
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text(text)))
				}, 300f)
			],
			pageNumber,
			null,
			pageWidth,
			pageHeight);
}