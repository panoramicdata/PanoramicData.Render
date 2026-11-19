using System.Globalization;
using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Tests for <see cref="ParagraphAligner"/>: computes X offsets for content
/// boxes on a line based on paragraph alignment mode.
/// Items use a compact notation: B&lt;w&gt; box, G&lt;w&gt;/&lt;stretch&gt;/&lt;shrink&gt; glue,
/// P&lt;w&gt;/&lt;value&gt;[/f] penalty (f = flagged), T = finishing glue plus forced break.
/// Expected boxes are written as index@x/width, with a leading ~ on x for an approximate match.
/// </summary>
public sealed class ParagraphAlignerTests
{
	private const string OneBox = "B100 P0/-inf";
	private const string TwoBoxes = "B100 G20/10/5 B80 T";
	private const string ThreeBoxes = "B100 G20/10/5 B80 G30/15/8 B60 T";

	private static float Num(string text) => text switch
	{
		"max" => float.MaxValue,
		"-inf" => float.NegativeInfinity,
		_ => float.Parse(text.TrimStart('~'), CultureInfo.InvariantCulture)
	};

	private static KnuthPlassItem[] ParseToken(string token)
	{
		var p = token[1..].Split('/');
		return token[0] switch
		{
			'B' => [new KnuthPlassBox(Num(p[0]))],
			'G' => [new KnuthPlassGlue(Num(p[0]), Num(p[1]), Num(p[2]))],
			'P' => [new KnuthPlassPenalty(Num(p[0]), Num(p[1]), p.Length > 2)],
			'T' => [new KnuthPlassGlue(0, float.MaxValue, 0), new KnuthPlassPenalty(0, float.NegativeInfinity, false)],
			_ => throw new ArgumentException(token)
		};
	}

	private static KnuthPlassItem[] Items(string notation) =>
		[.. notation.Split(' ').SelectMany(ParseToken)];

	private static KnuthPlassLine[] Lines(string spec) =>
		[.. spec.Split(';').Select(s => s.Split(':')).Select(p => new KnuthPlassLine(int.Parse(p[0], CultureInfo.InvariantCulture), int.Parse(p[1], CultureInfo.InvariantCulture), Num(p[2])))];

	private static ParagraphAlignment Align(string name) => Enum.Parse<ParagraphAlignment>(name);

	private static ParagraphIndentation Indent(float left, float right, float firstLine, float hanging) =>
		new(left, right, firstLine, hanging);

	private static void AssertBoxes(IReadOnlyList<PositionedBox> result, string expected)
	{
		var tokens = expected.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		result.Should().HaveCount(tokens.Length);
		for (var i = 0; i < tokens.Length; i++)
		{
			var parts = tokens[i].Split('@', '/');
			result[i].ItemIndex.Should().Be(int.Parse(parts[0], CultureInfo.InvariantCulture));
			if (parts[1].StartsWith('~'))
			{
				result[i].XOffset.Should().BeApproximately(Num(parts[1]), 0.01f);
			}
			else
			{
				result[i].XOffset.Should().Be(Num(parts[1]));
			}

			result[i].Width.Should().Be(Num(parts[2]));
		}
	}

	[Fact]
	public void NullItems_ThrowsArgumentNullException()
	{
		var act = () => ParagraphAligner.ComputeBoxPositions(
			null!, new KnuthPlassLine(0, 0, 0f), 1000f, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(-100f)]
	public void NonPositiveLineWidth_ThrowsArgumentOutOfRangeException(float lineWidth)
	{
		var act = () => ParagraphAligner.ComputeBoxPositions(
			Items(OneBox), new KnuthPlassLine(0, 0, 0f), lineWidth, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Theory]
	[InlineData(OneBox, 0, 1, 0f, 500f, "Left", false, "0@0/100")]
	[InlineData(TwoBoxes, 0, 4, 0f, 500f, "Left", false, "0@0/100 2@120/80")]
	[InlineData(ThreeBoxes, 0, 6, 0f, 1000f, "Left", false, "0@0/100 2@120/80 4@230/60")]
	[InlineData(OneBox, 0, 1, 0f, 500f, "Center", false, "0@200/100")]
	[InlineData(TwoBoxes, 0, 4, 0f, 500f, "Center", false, "0@150/100 2@270/80")]
	[InlineData(OneBox, 0, 1, 0f, 500f, "Right", false, "0@400/100")]
	[InlineData(TwoBoxes, 0, 4, 0f, 500f, "Right", false, "0@300/100 2@420/80")]
	[InlineData(TwoBoxes, 0, 4, 1.5f, 500f, "Justified", false, "0@0/100 2@135/80")]
	[InlineData(TwoBoxes, 0, 4, -0.5f, 500f, "Justified", false, "0@0/100 2@117.5/80")]
	[InlineData(TwoBoxes, 0, 4, 2.5f, 500f, "Justified", false, "0@0/100 2@120/80")]
	[InlineData(TwoBoxes, 0, 4, 0f, 500f, "Justified", false, "0@0/100 2@120/80")]
	[InlineData(ThreeBoxes, 0, 6, 1.0f, 1000f, "Justified", false, "0@0/100 2@130/80 4@255/60")]
	[InlineData("B100 P15/100/f B80", 0, 1, 0f, 500f, "Left", false, "0@0/100 1@100/15")]
	[InlineData(OneBox, 0, 1, 0f, 400f, "Left", false, "0@0/100")]
	[InlineData("P0/-inf", 0, 0, 0f, 500f, "Left", false, "")]
	[InlineData(OneBox, 0, 1, -1f, 50f, "Center", false, "0@0/100")]
	[InlineData(OneBox, 0, 1, -1f, 50f, "Right", false, "0@0/100")]
	[InlineData("B100 G20/10/5 B80 G25/12/6 B60 T", 2, 6, 0f, 1000f, "Left", false, "2@0/80 4@105/60")]
	[InlineData("B100 P0/50 B80 T", 0, 4, 0f, 500f, "Left", false, "0@0/100 2@100/80")]
	[InlineData("B200 P0/-inf", 0, 1, 0f, 500f, "Left", false, "0@0/200")]
	[InlineData("B200 P0/-inf", 0, 1, 0f, 500f, "Center", false, "0@150/200")]
	[InlineData("B200 P0/-inf", 0, 1, 0f, 500f, "Right", false, "0@300/200")]
	[InlineData("B200 P0/-inf", 0, 1, 0f, 500f, "Justified", false, "0@0/200")]
	[InlineData(TwoBoxes, 0, 4, 1.5f, 500f, "Justified", true, "0@0/100 2@120/80")]
	[InlineData(TwoBoxes, 0, 4, 1.5f, 600f, "Justified", false, "0@0/100 2@135/80")]
	[InlineData(OneBox, 0, 1, 0f, 500f, "Left", true, "0@0/100")]
	[InlineData(OneBox, 0, 1, 0f, 500f, "Center", true, "0@200/100")]
	[InlineData(OneBox, 0, 1, 0f, 500f, "Right", true, "0@400/100")]
	public void ComputeBoxPositions_ReturnsExpectedBoxes(
		string items, int start, int end, float ratio, float lineWidth, string alignment, bool isLastLine, string expected)
	{
		var result = ParagraphAligner.ComputeBoxPositions(
			Items(items), new KnuthPlassLine(start, end, ratio), lineWidth, Align(alignment), isLastLine: isLastLine);

		AssertBoxes(result, expected);
	}

	[Theory]
	[InlineData(OneBox, 500f, "Left", 200f, 0f, 0f, 0f, "0@200/100", "")]
	[InlineData(OneBox, 500f, "Center", 0f, 100f, 0f, 0f, "0@150/100", "")]
	[InlineData(OneBox, 500f, "Right", 0f, 100f, 0f, 0f, "0@300/100", "")]
	[InlineData(OneBox, 500f, "Left", 200f, 0f, 100f, 0f, "0@200/100", "0@300/100")]
	[InlineData("B50 P0/-inf", 1000f, "Left", 200f, 0f, 0f, 300f, "0@500/50", "0@200/50")]
	[InlineData(OneBox, 500f, "Center", 100f, 100f, 0f, 0f, "0@200/100", "")]
	[InlineData(OneBox, 500f, "Left", 300f, 300f, 0f, 0f, "0@300/100", "")]
	public void ComputeBoxPositions_AppliesIndentation(
		string items, float lineWidth, string alignment, float left, float right, float firstLine, float hanging, string subsequent, string first)
	{
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = Indent(left, right, firstLine, hanging);

		var subsequentResult = ParagraphAligner.ComputeBoxPositions(
			Items(items), line, lineWidth, Align(alignment), indentation: indent, isFirstLine: false);
		AssertBoxes(subsequentResult, subsequent);

		if (first.Length == 0)
		{
			return;
		}

		var firstResult = ParagraphAligner.ComputeBoxPositions(
			Items(items), line, lineWidth, Align(alignment), indentation: indent, isFirstLine: true);
		AssertBoxes(firstResult, first);
	}

	[Fact]
	public void ParagraphPositions_NullItems_Throws()
	{
		var act = () => ParagraphAligner.ComputeParagraphBoxPositions(
			null!, [], 500f, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParagraphPositions_NullLines_Throws()
	{
		var act = () => ParagraphAligner.ComputeParagraphBoxPositions(
			[], null!, 500f, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(-1f)]
	public void ParagraphPositions_NonPositiveLineWidth_Throws(float width)
	{
		var act = () => ParagraphAligner.ComputeParagraphBoxPositions(
			[], [], width, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ParagraphPositions_EmptyLines_ReturnsEmpty()
	{
		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			Array.Empty<KnuthPlassItem>(), Array.Empty<KnuthPlassLine>(), 500f, ParagraphAlignment.Left);

		result.Should().BeEmpty();
	}

	[Theory]
	[InlineData(TwoBoxes, "0:4:1.5", 500f, "Justified", 0f, 0f, 0f, 0f, "0@0/100 2@120/80")]
	[InlineData("B100 G20/10/5 B80 G20/10/5 B60 T", "0:3:1;4:6:0", 500f, "Justified", 0f, 0f, 0f, 0f, "0@0/100 2@130/80|4@0/60")]
	[InlineData(
		"B100 G20/10/5 B80 G20/10/5 B90 G25/12/6 B70 G25/12/6 B50 G15/8/4 B40 T",
		"0:3:0.5;4:7:0.8;8:12:0", 500f, "Justified", 0f, 0f, 0f, 0f,
		"0@0/100 2@125/80|4@0/90 6@~124.6/70|8@0/50 10@65/40")]
	[InlineData(TwoBoxes, "0:4:0", 500f, "Center", 0f, 0f, 0f, 0f, "0@150/100 2@270/80")]
	[InlineData("B50 G20/10/5 B50 T", "0:1:0;2:4:0", 1000f, "Left", 200f, 0f, 0f, 300f, "0@200/50|2@500/50")]
	[InlineData("B50 G20/10/5 B50 T", "0:1:0;2:4:0", 1000f, "Left", 100f, 0f, 150f, 0f, "0@250/50|2@100/50")]
	public void ComputeParagraphBoxPositions_ReturnsExpectedLines(
		string items, string lines, float lineWidth, string alignment, float left, float right, float firstLine, float hanging, string expected)
	{
		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			Items(items), Lines(lines), lineWidth, Align(alignment), Indent(left, right, firstLine, hanging));

		var expectedLines = expected.Split('|');
		result.Should().HaveCount(expectedLines.Length);
		for (var i = 0; i < expectedLines.Length; i++)
		{
			AssertBoxes(result[i], expectedLines[i]);
		}
	}
}
