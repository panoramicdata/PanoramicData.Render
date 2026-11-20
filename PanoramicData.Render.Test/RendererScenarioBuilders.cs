namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static class RendererScenarioBuilders
{
	internal static LayoutPage ParagraphPage(Paragraph paragraph)
		=> TestLayoutPageFactory.Create([ParagraphLayoutBlock(paragraph)], 1);

	internal static LayoutPage BiDiPage(Paragraph paragraph)
		=> TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true }, 300f)], 1);

	internal static LayoutPage BodyPageWithWatermark(WatermarkInfo watermark)
		=> TestLayoutPageFactory.Create([ParagraphLayoutBlock(new Paragraph(new Run(new Text("Body"))))], 1, watermark: watermark);

	internal static LayoutPage BookmarkedPage(string text, string bookmark)
		=> TestLayoutPageFactory.Create([BookmarkedBlock(text, bookmark)], 1);

	internal static LayoutPage BookmarkAndLinkPage(string targetText, string linkText, string bookmark)
	{
		var link = new Paragraph(new Hyperlink(new Run(new Text(linkText))) { Anchor = bookmark });
		return TestLayoutPageFactory.Create([BookmarkedBlock(targetText, bookmark), ParagraphLayoutBlock(link)], 1);
	}

	internal static LayoutPage RightTabHeaderPage(string title)
	{
		var tabs = new Tabs(new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Right, Position = 9360 });
		var header = new Paragraph(new ParagraphProperties(tabs), PreservedRun(title), new Run(new TabChar()), PreservedRun("Page 1"));
		return new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1440,
			HeaderTopTwips = 720,
			HeaderBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = header }, 240f)],
			Blocks = [ParagraphLayoutBlock(new Paragraph(new Run(new Text("Body"))))]
		};
	}

	internal static Paragraph DotLeaderTabParagraph(string left, string right)
	{
		var tab = new DocumentFormat.OpenXml.Wordprocessing.TabStop
		{
			Val = TabStopValues.Right,
			Position = 9360,
			Leader = TabStopLeaderCharValues.Dot
		};
		return new Paragraph(new ParagraphProperties(new Tabs(tab)), PreservedRun(left), new Run(new TabChar()), PreservedRun(right));
	}

	internal static Paragraph HyperlinkParagraph(string text, string anchor)
		=> new(new Hyperlink(new Run(new Text(text))) { Anchor = anchor });

	internal static Paragraph BiDiParagraph(string rtlText)
		=> new(new ParagraphProperties(new BiDi()), RtlRun(new Text(rtlText)));

	internal static Paragraph MixedBiDiParagraph()
		=> new(new ParagraphProperties(new BiDi()), RtlRun(new Text("שלום") { Space = SpaceProcessingModeValues.Preserve }), PreservedRun(" Hello"));

	internal static Paragraph InlineSdtParagraph()
		=> new(SdtRunOf("SDT Content"));

	internal static Paragraph SdtMixedWithNormalParagraph()
		=> new(PreservedRun("Normal "), SdtRunOf("Controlled"));

	internal static WatermarkInfo TextWatermark(string text, string fontFamily, string fillColor, float opacity)
		=> new()
		{
			Kind = WatermarkKind.Text,
			Text = text,
			FontFamily = fontFamily,
			FillColor = fillColor,
			Opacity = opacity,
			RotationDegrees = 315f,
			WidthTwips = 8000f,
			HeightTwips = 2000f
		};

	internal static WatermarkInfo ImageWatermark(byte[] imageBytes)
		=> new()
		{
			Kind = WatermarkKind.Image,
			ResolvedImageData = new ImageData(imageBytes, "image/png"),
			Opacity = 0.5f,
			WidthTwips = 7200f,
			HeightTwips = 5400f
		};

	internal static Run[] FieldRuns(string instruction, string cachedValue)
		=>
		[
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = instruction }),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text(cachedValue)),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End })
		];

	private static LayoutBlock ParagraphLayoutBlock(Paragraph paragraph)
		=> new(new ParagraphBlock { SourceElement = paragraph }, 300f);

	private static LayoutBlock BookmarkedBlock(string text, string bookmark)
		=> new(new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text(text))),
			BookmarkStarts = [new BookmarkStartInfo(1, bookmark)]
		}, 300f);

	private static Run PreservedRun(string text)
		=> new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

	private static Run RtlRun(Text text)
		=> new(new RunProperties(new RightToLeftText()), text);

	private static SdtRun SdtRunOf(string text)
		=> new(new SdtContentRun(new Run(new Text(text))));
}
