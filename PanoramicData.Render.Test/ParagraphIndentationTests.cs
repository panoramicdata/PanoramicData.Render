using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Tests for <see cref="ParagraphIndentation"/> record struct.
/// </summary>
public sealed class ParagraphIndentationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NoneAndDefault_AllZero(bool useNone)
	{
		var indent = useNone ? ParagraphIndentation.None : new ParagraphIndentation();

		(indent.Left, indent.Right, indent.FirstLine, indent.Hanging).Should().Be((0f, 0f, 0f, 0f));
	}

	[Theory]
	[InlineData(0f, 0f, 0f, 0f)]
	[InlineData(720f, 0f, 0f, 720f)]
	[InlineData(720f, 360f, 0f, 1080f)]
	[InlineData(720f, 0f, 360f, 720f)]
	public void GetFirstLineLeftIndent_ReturnsExpected(float left, float firstLine, float hanging, float expected)
	{
		var indent = new ParagraphIndentation(Left: left, FirstLine: firstLine, Hanging: hanging);

		indent.GetFirstLineLeftIndent().Should().Be(expected);
	}

	[Theory]
	[InlineData(0f, 0f, 0f, 0f)]
	[InlineData(720f, 0f, 0f, 720f)]
	[InlineData(720f, 360f, 0f, 720f)]
	[InlineData(720f, 0f, 360f, 1080f)]
	public void GetSubsequentLineLeftIndent_ReturnsExpected(float left, float firstLine, float hanging, float expected)
	{
		var indent = new ParagraphIndentation(Left: left, FirstLine: firstLine, Hanging: hanging);

		indent.GetSubsequentLineLeftIndent().Should().Be(expected);
	}

	[Theory]
	[InlineData(100f, true)]
	[InlineData(200f, false)]
	public void Equality_ComparesAllValues(float otherLeft, bool expectedEqual)
	{
		var a = new ParagraphIndentation(Left: 100f, Right: 200f, FirstLine: 50f);
		var b = new ParagraphIndentation(Left: otherLeft, Right: 200f, FirstLine: 50f);

		a.Equals(b).Should().Be(expectedEqual);
	}
}
