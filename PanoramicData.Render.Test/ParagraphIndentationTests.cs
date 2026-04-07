using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Tests for <see cref="ParagraphIndentation"/> record struct.
/// </summary>
public sealed class ParagraphIndentationTests
{
	[Fact]
	public void None_AllZero()
	{
		var indent = ParagraphIndentation.None;

		indent.Left.Should().Be(0f);
		indent.Right.Should().Be(0f);
		indent.FirstLine.Should().Be(0f);
		indent.Hanging.Should().Be(0f);
	}

	[Fact]
	public void Default_AllZero()
	{
		var indent = new ParagraphIndentation();

		indent.Left.Should().Be(0f);
		indent.Right.Should().Be(0f);
		indent.FirstLine.Should().Be(0f);
		indent.Hanging.Should().Be(0f);
	}

	// ===================================================================
	// GetFirstLineLeftIndent
	// ===================================================================

	[Fact]
	public void GetFirstLineLeftIndent_NoIndentation_ReturnsZero()
	{
		var indent = new ParagraphIndentation();
		indent.GetFirstLineLeftIndent().Should().Be(0f);
	}

	[Fact]
	public void GetFirstLineLeftIndent_LeftOnly_ReturnsLeft()
	{
		var indent = new ParagraphIndentation(Left: 720f);
		indent.GetFirstLineLeftIndent().Should().Be(720f);
	}

	[Fact]
	public void GetFirstLineLeftIndent_LeftPlusFirstLine_ReturnsSum()
	{
		// Left=720 + FirstLine=360 = 1080
		var indent = new ParagraphIndentation(Left: 720f, FirstLine: 360f);
		indent.GetFirstLineLeftIndent().Should().Be(1080f);
	}

	[Fact]
	public void GetFirstLineLeftIndent_WithHanging_ReturnsLeftOnly()
	{
		// Hanging indent: first line is at Left (not further indented)
		var indent = new ParagraphIndentation(Left: 720f, Hanging: 360f);
		indent.GetFirstLineLeftIndent().Should().Be(720f);
	}

	// ===================================================================
	// GetSubsequentLineLeftIndent
	// ===================================================================

	[Fact]
	public void GetSubsequentLineLeftIndent_NoIndentation_ReturnsZero()
	{
		var indent = new ParagraphIndentation();
		indent.GetSubsequentLineLeftIndent().Should().Be(0f);
	}

	[Fact]
	public void GetSubsequentLineLeftIndent_LeftOnly_ReturnsLeft()
	{
		var indent = new ParagraphIndentation(Left: 720f);
		indent.GetSubsequentLineLeftIndent().Should().Be(720f);
	}

	[Fact]
	public void GetSubsequentLineLeftIndent_FirstLineSet_ReturnsLeft()
	{
		// FirstLine only affects line 1; subsequent lines get just Left
		var indent = new ParagraphIndentation(Left: 720f, FirstLine: 360f);
		indent.GetSubsequentLineLeftIndent().Should().Be(720f);
	}

	[Fact]
	public void GetSubsequentLineLeftIndent_WithHanging_ReturnsLeftPlusHanging()
	{
		// Subsequent lines get Left + Hanging
		var indent = new ParagraphIndentation(Left: 720f, Hanging: 360f);
		indent.GetSubsequentLineLeftIndent().Should().Be(1080f);
	}

	// ===================================================================
	// Record equality
	// ===================================================================

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new ParagraphIndentation(Left: 100f, Right: 200f, FirstLine: 50f);
		var b = new ParagraphIndentation(Left: 100f, Right: 200f, FirstLine: 50f);

		a.Should().Be(b);
	}

	[Fact]
	public void Equality_DifferentValues_AreNotEqual()
	{
		var a = new ParagraphIndentation(Left: 100f);
		var b = new ParagraphIndentation(Left: 200f);

		a.Should().NotBe(b);
	}
}
