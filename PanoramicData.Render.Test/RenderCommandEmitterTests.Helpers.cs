namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using static PanoramicData.Render.Test.EmitterBuilders2;
using FakeRenderTarget = PanoramicData.Render.Test.RenderCommandEmitterTests.FakeRenderTarget;

internal static class EmitterBuilders
{
	internal static SdtRun SdtRunOf(string text)
		=> new(new SdtProperties(), new SdtContentRun(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve })));

	internal static Paragraph AlignedParagraph(string text, params OpenXmlElement[] properties)
		=> new(new ParagraphProperties(properties), new Run(new Text(text)));

	internal static SectionInfo StandardSection() => new() { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 };

	internal static LayoutPage StandardPage(int pageNumber, params LayoutBlock[] blocks)
		=> TestLayoutPageFactory.Create(blocks, pageNumber, section: StandardSection());

	internal static LayoutPage BookmarkPage(params BookmarkStartInfo[] bookmarks)
		=> TestLayoutPageFactory.Create(
			[new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("Section"))), BookmarkStarts = bookmarks }, 300f)],
			1,
			section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });

	internal static FakeRenderTarget EmitField(Paragraph paragraph, int pageNumber = 1, int? totalPageCount = null, DateTime? renderTimestampUtc = null)
		=> EmitParagraph(paragraph, NarrowSection(), pageNumber, null, totalPageCount, renderTimestampUtc);

	internal static void AssertCachedField(string cachedText, string instruction)
		=> AssertSingleText(EmitField(SimpleFieldParagraph(cachedText, instruction)), cachedText);

	internal static void AssertHyperlink(FakeRenderTarget target, string text, string uri)
	{
		AssertSingleText(target, text);
		target.HyperlinkCalls.Should().ContainSingle();
		target.HyperlinkCalls[0].Uri.Should().Be(uri);
	}

	internal static void AssertTextsInOrder(FakeRenderTarget target, params string[] texts)
		=> target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder(texts);

	internal static void AssertWatermarkFirst(FakeRenderTarget target, string watermarkCall)
	{
		target.CallOrder.Count.Should().BeGreaterThanOrEqualTo(2);
		target.CallOrder[0].Should().StartWith(watermarkCall);
		target.CallOrder[1].Should().StartWith("DrawText:");
	}

	internal static ImageData PngImage() => new([0x89, 0x50, 0x4E, 0x47], "image/png");

	internal static RenderOptions NumberingOptions(params (string Key, NumberingLevelStyle Style)[] styles)
	{
		var options = new RenderOptions();
		foreach (var (key, style) in styles)
		{
			options.NumberingStyles[key] = style;
		}

		return options;
	}

	internal static NumberingLevelStyle NumberLevel(int level, string levelText, string numberFormat = "decimal", string? fontFamily = null, int? restartAfterLevel = null)
		=> new() { LevelIndex = level, Start = 1, NumberFormat = numberFormat, LevelText = levelText, FontFamily = fontFamily, RestartAfterLevel = restartAfterLevel };

	internal static SectionInfo NarrowSection() => new() { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 };

	internal static FakeRenderTarget Emit(
		LayoutPage page,
		RenderOptions? options = null,
		int? totalPageCount = null,
		DateTime? renderTimestampUtc = null,
		IReadOnlyDictionary<string, ImageData>? images = null)
	{
		var target = new FakeRenderTarget();
		RenderCommandEmitter.EmitPage(page, target, options, totalPageCount, renderTimestampUtc, images: images);
		return target;
	}

	internal static FakeRenderTarget EmitParagraph(
		Paragraph paragraph,
		SectionInfo? section = null,
		int pageNumber = 1,
		RenderOptions? options = null,
		int? totalPageCount = null,
		DateTime? renderTimestampUtc = null,
		IReadOnlyDictionary<string, ImageData>? images = null)
	{
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)], pageNumber, section: section);
		return Emit(page, options, totalPageCount, renderTimestampUtc, images);
	}
}
