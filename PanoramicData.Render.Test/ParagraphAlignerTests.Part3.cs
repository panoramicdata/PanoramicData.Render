using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed partial class ParagraphAlignerTests
{
	[Fact]
	public void ExtremeIndentation_EffectiveWidthClamped()
	{
		// Left+Right > lineWidth → effective=1 (clamped), content overflows
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Left: 300f, Right: 300f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Left,
			indentation: indent);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(300f); // Still starts at left indent
	}
}
