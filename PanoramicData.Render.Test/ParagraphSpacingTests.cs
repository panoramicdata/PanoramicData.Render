using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class ParagraphSpacingTests
{
	private static ParagraphSpacing Spacing(float before, float after, float twips, string rule) =>
		new(before, after, twips, rule.Length == 0 ? null : Enum.Parse<LineSpacingRule>(rule));

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void NoneAndDefault_AllZeroDefaults(bool useNone)
	{
		var spacing = useNone ? ParagraphSpacing.None : new ParagraphSpacing();

		(spacing.SpaceBefore, spacing.SpaceAfter, spacing.LineSpacingTwips).Should().Be((0f, 0f, 0f));
		spacing.LineRule.Should().BeNull();
		spacing.EffectiveLineRule.Should().Be(LineSpacingRule.Auto);
	}

	[Theory]
	[InlineData("", "Auto")]
	[InlineData("Exact", "Exact")]
	[InlineData("AtLeast", "AtLeast")]
	public void EffectiveLineRule_ResolvesRule(string rule, string expected)
	{
		Spacing(0f, 0f, 0f, rule).EffectiveLineRule.Should().Be(Enum.Parse<LineSpacingRule>(expected));
	}

	[Theory]
	[InlineData(0f, 1f, 0f)]
	[InlineData(-100f, 1f, 0f)]
	[InlineData(240f, 1f, 0f)]
	[InlineData(360f, 1.5f, 0f)]
	[InlineData(480f, 2f, 0f)]
	[InlineData(276f, 1.15f, 0.001f)]
	public void GetLineSpacingMultiplier_ReturnsExpected(float twips, float expected, float tolerance)
	{
		Spacing(0f, 0f, twips, string.Empty).GetLineSpacingMultiplier().Should().BeApproximately(expected, tolerance);
	}

	[Theory]
	[InlineData(0f, "", 300f)]
	[InlineData(240f, "", 300f)]
	[InlineData(480f, "", 600f)]
	[InlineData(360f, "", 450f)]
	[InlineData(200f, "Exact", 200f)]
	[InlineData(100f, "Exact", 100f)]
	[InlineData(500f, "Exact", 500f)]
	[InlineData(0f, "Exact", 300f)]
	[InlineData(400f, "AtLeast", 400f)]
	[InlineData(200f, "AtLeast", 300f)]
	[InlineData(300f, "AtLeast", 300f)]
	[InlineData(0f, "AtLeast", 300f)]
	public void ComputeLineHeight_AppliesRuleToNaturalHeight(float twips, string rule, float expected)
	{
		Spacing(0f, 0f, twips, rule).ComputeLineHeight(300f).Should().Be(expected);
	}

	[Theory]
	[InlineData(100f, 200f, 240f, "", 0, 0f)]
	[InlineData(100f, 200f, 0f, "", 1, 600f)]
	[InlineData(120f, 120f, 480f, "", 3, 2040f)]
	[InlineData(50f, 50f, 200f, "Exact", 2, 500f)]
	[InlineData(100f, 200f, 0f, "", -1, 0f)]
	[InlineData(0f, 0f, 0f, "", 4, 1200f)]
	public void ComputeParagraphHeight_ReturnsExpected(float before, float after, float twips, string rule, int lines, float expected)
	{
		Spacing(before, after, twips, rule).ComputeParagraphHeight(lines, 300f).Should().Be(expected);
	}

	[Theory]
	[InlineData("AtLeast", true)]
	[InlineData("Exact", false)]
	public void RecordEquality_ComparesAllValues(string otherRule, bool expectedEqual)
	{
		var a = Spacing(100f, 200f, 360f, "AtLeast");
		var b = Spacing(100f, 200f, 360f, otherRule);

		a.Equals(b).Should().Be(expectedEqual);
	}
}
