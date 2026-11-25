using Xunit;

namespace PanoramicData.Render.Test;

public sealed class FountainRendererTests
{
	private const string Stage = """
		Title: A Small Light
		Author: Jules Poster

		>CHARACTERS<

		ANA - the caretaker

		>SETTING<

		A closed library.

		>TIME<

		After sunset.

		>ACT I<

		.SCENE 1

		A light glows in the window.

		@ANA
		(quietly)
		I found the key.

		.SCENE 2

		A door closes.
		""";

	private const string Television = """
		Title: The Station
		Author: Jules Poster

		>ACT ONE<

		FADE IN:

		INT. STATION - NIGHT

		The clock stops.

		@ANA (V.O.)
		(quietly)
		I missed the last train.

		>END OF ACT ONE<

		>ACT TWO<

		EXT. PLATFORM - NIGHT

		A light flickers.
		""";

	[Theory]
	[InlineData(FountainRenderProfile.BbcStageUs, 5)]
	[InlineData(FountainRenderProfile.BbcStageUk, 4)]
	[InlineData(FountainRenderProfile.BbcTelevision, 3)]
	[InlineData(FountainRenderProfile.FeatureScreenplay, 2)]
	public void RenderPdf_GeneratesExpectedPageCount(FountainRenderProfile profile, int pageCount)
	{
		var source = profile is FountainRenderProfile.BbcStageUs or FountainRenderProfile.BbcStageUk ? Stage : Television;
		var pdf = new FountainRenderer().RenderPdf(source, profile, TestContext.Current.CancellationToken);

		var pdfText = System.Text.Encoding.Latin1.GetString(pdf);
		Assert.StartsWith("%PDF-", pdfText);
		Assert.Equal(pageCount, System.Text.RegularExpressions.Regex.Count(pdfText, @"/Type\s*/Page\b"));
	}

	[Fact]
	public void StageLayouts_PlaceTheSameScriptUnderDifferentBbcRules()
	{
		var us = Layout(Stage, FountainRenderProfile.BbcStageUs);
		var uk = Layout(Stage, FountainRenderProfile.BbcStageUk);
		Assert.Equal(new int?[] { null, null, null, 1, 2 }, us.Select(static page => page.BodyPageNumber));
		Assert.Equal(new int?[] { null, null, 1, 2 }, uk.Select(static page => page.BodyPageNumber));
		Assert.Equal("1.", us[3].Lines[0].Text);
		Assert.Equal(44.8f, us[3].Lines[0].Baseline);
		Assert.Equal("-1-", uk[2].Lines[0].Text);
		Assert.Equal(804, uk[2].Lines[0].Baseline);

		var usFirst = us[3].Lines;
		var ukFirst = uk[2].Lines;
		Assert.Equal(288, Find(usFirst, "ACT I").X);
		Assert.True(Find(usFirst, "SCENE 1").Baseline > Find(usFirst, "ACT I").Baseline);
		Assert.Equal(Find(ukFirst, "ACT I").Baseline, Find(ukFirst, "SCENE 1").Baseline);
		Assert.Equal(180, Find(ukFirst, "SCENE 1").X);
		Assert.Equal(108, Find(usFirst, "I found the key.").X);
		Assert.Equal(288, Find(usFirst, "ANA").X);
		Assert.True(Find(usFirst, "ANA").Baseline < Find(usFirst, "I found the key.").Baseline);
		Assert.Equal(72, Find(ukFirst, "ANA: (QUIETLY) I found the key.").X);
		Assert.Equal(288, Find(usFirst, "(A light glows in the window.)").X);
		Assert.Equal(252, Find(ukFirst, "A LIGHT GLOWS IN THE WINDOW.").X);
		Assert.Equal(216, Find(usFirst, "(quietly)").X);
		Assert.Equal(Find(us[4].Lines, "SCENE 2").X, Find(usFirst, "SCENE 1").X);
		Assert.Contains(uk[3].Lines, static line => line.Text == "SCENE 2");
	}

	[Fact]
	public void UsStageLayout_ContinuesAtSentenceBoundaryWithParenthetical()
	{
		var dialogue = Enumerable.Range(1, 50).Select(static index => $"Beat {index}.");
		var source = $".SCENE 1\n\n@LENA\n{string.Join('\n', dialogue)}\n(remembering)\nAnd now for the finish.";
		var pages = Layout(source, FountainRenderProfile.BbcStageUs);

		Assert.Equal(2, pages.Count);
		Assert.Equal("Beat 50.", pages[0].Lines[^1].Text);
		Assert.Equal("LENA (Continued)", pages[1].Lines[1].Text);
		Assert.Equal("(remembering)", pages[1].Lines[2].Text);
		Assert.Equal("And now for the finish.", pages[1].Lines[3].Text);
	}

	[Fact]
	public void TelevisionLayout_StartsNewActAndFeatureDoesNotGenerateAnAct()
	{
		var tv = Layout(Television, FountainRenderProfile.BbcTelevision);
		var feature = Layout(Television, FountainRenderProfile.FeatureScreenplay);

		Assert.Equal(3, tv.Count);
		Assert.Null(tv[0].BodyPageNumber);
		Assert.Equal("1.", tv[1].Lines[0].Text);
		Assert.Contains(tv[1].Lines, static line => line.Text == "ACT ONE" && line.Underline);
		Assert.Contains(tv[1].Lines, static line => line.Text == "INT. STATION - NIGHT" && line.X == 108);
		Assert.Contains(tv[1].Lines, static line => line.Text == "ANA (V.O.)" && line.X == 252);
		Assert.Contains(tv[1].Lines, static line => line.Text == "(quietly)" && line.X == 216);
		Assert.Contains(tv[1].Lines, static line => line.Text == "I missed the last train." && line.X == 180);
		Assert.Contains(tv[2].Lines, static line => line.Text == "ACT TWO");
		Assert.Contains(tv[2].Lines, static line => line.Text == "FADE IN:");

		Assert.Equal(2, feature.Count);
		Assert.DoesNotContain(feature[1].Lines, static line => line.Text == "1.");
		Assert.Single(feature[1].Lines, static line => line.Text == "FADE IN:");
		Assert.Contains(feature[1].Lines, static line => line.Text == "ACT TWO" && !line.Underline);
	}


	[Fact]
	public void Parser_HonoursStandardScriptElementsWithoutRewritingSource()
	{
		const string source = "\uFEFFTitle: Small Things\r\nAuthor: Jules Poster\r\nContact:\r\n    hello@example.com\r\n\r\n# Outline\r\n= Synopsis\r\n\r\n>ACT I<\r\n\r\n.INT. SHED - DAY #1#\r\n\r\n!ANNOUNCEMENT\r\n\r\n/* HIDDEN */\r\n@Jo (V.O.)\r\n(waiting)\r\nCome in. [[private]]\r\n\r\n===\r\n\r\n>THE END<";
		var script = FountainScriptParser.Parse(source, TestContext.Current.CancellationToken);

		Assert.Equal("hello@example.com", script.TitlePage["Contact"]);
		Assert.Equal(
			[FountainBlockKind.Centered, FountainBlockKind.Scene, FountainBlockKind.Action, FountainBlockKind.Dialogue, FountainBlockKind.PageBreak, FountainBlockKind.Centered],
			script.Blocks.Select(static block => block.Kind));
		Assert.Equal("INT. SHED - DAY", script.Blocks[1].Text);
		Assert.Equal("ANNOUNCEMENT", script.Blocks[2].Text);
		Assert.Equal("Jo (V.O.)", script.Blocks[3].Text);
		var speech = Assert.IsAssignableFrom<IReadOnlyList<FountainSpeechLine>>(script.Blocks[3].Speech);
		Assert.Equal(new FountainSpeechLine("(waiting)", true), speech[0]);
		Assert.Equal(new FountainSpeechLine("Come in.", false), speech[1]);
		Assert.StartsWith("\uFEFFTitle:", source);
	}

	[Fact]
	public void RenderPdf_RespectsPageRangeWithoutRepaginating()
	{
		var options = new RenderOptions { PageRange = 1..2 };
		var renderer = new FountainRenderer(options);
		var pdf = renderer.RenderPdf(Stage, FountainRenderProfile.BbcStageUs, TestContext.Current.CancellationToken);
		Assert.Equal(1, System.Text.RegularExpressions.Regex.Count(System.Text.Encoding.Latin1.GetString(pdf), @"/Type\s*/Page\b"));
		options.PageRange = 3..3;
		Assert.Empty(renderer.RenderPdf(Stage, FountainRenderProfile.BbcStageUs, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void RenderPdf_ValidatesArgumentsAndCancellation()
	{
		var renderer = new FountainRenderer();
		Assert.Throws<ArgumentNullException>(() => renderer.RenderPdf(null!, FountainRenderProfile.BbcStageUs, TestContext.Current.CancellationToken));
		Assert.Throws<ArgumentOutOfRangeException>(() => renderer.RenderPdf(Stage, (FountainRenderProfile)99, TestContext.Current.CancellationToken));
		Assert.Throws<OperationCanceledException>(() => renderer.RenderPdf(Stage, FountainRenderProfile.BbcStageUs, new CancellationToken(true)));
	}


	[Fact]
	public void TelevisionLayout_RespectsExplicitBreaksAndSideBySideDialogue()
	{
		const string source = "INT. STATION - NIGHT\n\nANA\nWait for me.\n\nSAM ^\n(whispering)\nNot today.\n\n===\n\n>SMASH TO:\n\n~The promised song";
		var pages = Layout(source, FountainRenderProfile.BbcTelevision);

		Assert.Equal(2, pages.Count);
		Assert.Equal(Find(pages[0].Lines, "ANA").Baseline, Find(pages[0].Lines, "SAM").Baseline);
		Assert.True(Find(pages[0].Lines, "Wait for me.").X < Find(pages[0].Lines, "Not today.").X);
		Assert.Contains(pages[1].Lines, static line => line.Text == "SMASH TO:");
		Assert.Contains(pages[1].Lines, static line => line.Text == "The promised song");
	}

	[Theory]
	[InlineData(FountainRenderProfile.BbcStageUs, "ANA (Continued)")]
	[InlineData(FountainRenderProfile.BbcStageUk, "A final line.")]
	[InlineData(FountainRenderProfile.BbcTelevision, "ANA (CONT'D)")]
	public void Layout_ContinuesTheSameSpeakerAfterAnInterruption(FountainRenderProfile profile, string expected)
	{
		const string source = "ANA\nThe doorbell rang.\n\nSomeone opens the door.\n\nANA\nA final line.";
		var pages = Layout(source, profile);
		Assert.Contains(pages[0].Lines, line => line.Text == expected);
	}

	[Fact]
	public void TelevisionLayout_PaintsMoreCueAndContinuesSpeechAcrossPages()
	{
		var dialogue = Enumerable.Repeat("She listens for the rest of the announcement.", 120);
		var source = $"@ANA\n{string.Join('\n', dialogue)}";
		var pages = Layout(source, FountainRenderProfile.BbcTelevision);

		Assert.True(pages.Count > 2);
		Assert.Equal("(MORE)", pages[0].Lines[^1].Text);
		Assert.Equal("ANA (CONT'D)", pages[1].Lines[1].Text);
		Assert.All(pages, static page => Assert.True(page.Lines.Count > 2));
	}

	[Fact]
	public void Layout_PaginatesTitleOnlyScriptWithoutANumberedBlankPage()
	{
		var pages = Layout("Title: Small Things\nContact:\n    hello@example.com", FountainRenderProfile.BbcTelevision);
		var page = Assert.Single(pages);
		Assert.Null(page.BodyPageNumber);
		Assert.Contains(page.Lines, static line => line.Text == "Small Things");
		Assert.Contains(page.Lines, static line => line.Text == "hello@example.com");
	}

	private static IReadOnlyList<FountainPdfPage> Layout(string source, FountainRenderProfile profile)
	{
		using var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 12);
		var script = FountainScriptParser.Parse(source, TestContext.Current.CancellationToken);
		return new FountainPageBuilder(FountainPdfLayout.For(profile), new FountainLineFormatter(font), TestContext.Current.CancellationToken).Build(script);
	}

	private static FountainPdfLine Find(IEnumerable<FountainPdfLine> page, string text) => Assert.Single(page, line => line.Text == text);

}
