using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

public class ParagraphBordersTests
{
	// ===================================================================
	// Defaults
	// ===================================================================

	[Fact]
	public void None_AllEdgesNull()
	{
		var borders = ParagraphBorders.None;

		borders.Top.Should().BeNull();
		borders.Bottom.Should().BeNull();
		borders.Left.Should().BeNull();
		borders.Right.Should().BeNull();
		borders.Between.Should().BeNull();
		borders.Bar.Should().BeNull();
	}

	[Fact]
	public void Default_MatchesNone()
	{
		var borders = new ParagraphBorders();
		borders.Should().Be(ParagraphBorders.None);
	}

	// ===================================================================
	// HasAnyVisibleBorder
	// ===================================================================

	[Fact]
	public void HasAnyVisibleBorder_None_ReturnsFalse()
	{
		ParagraphBorders.None.HasAnyVisibleBorder.Should().BeFalse();
	}

	[Fact]
	public void HasAnyVisibleBorder_AllNoneStyle_ReturnsFalse()
	{
		var borders = new ParagraphBorders(
			Top: new ParagraphBorder(BorderStyle.None),
			Bottom: new ParagraphBorder(BorderStyle.None));

		borders.HasAnyVisibleBorder.Should().BeFalse();
	}

	[Fact]
	public void HasAnyVisibleBorder_TopVisible_ReturnsTrue()
	{
		var borders = new ParagraphBorders(Top: new ParagraphBorder(BorderStyle.Single));
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	[Fact]
	public void HasAnyVisibleBorder_BottomVisible_ReturnsTrue()
	{
		var borders = new ParagraphBorders(Bottom: new ParagraphBorder(BorderStyle.Dotted));
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	[Fact]
	public void HasAnyVisibleBorder_LeftVisible_ReturnsTrue()
	{
		var borders = new ParagraphBorders(Left: new ParagraphBorder(BorderStyle.Dashed));
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	[Fact]
	public void HasAnyVisibleBorder_RightVisible_ReturnsTrue()
	{
		var borders = new ParagraphBorders(Right: new ParagraphBorder(BorderStyle.Double));
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	[Fact]
	public void HasAnyVisibleBorder_BetweenVisible_ReturnsTrue()
	{
		var borders = new ParagraphBorders(Between: new ParagraphBorder(BorderStyle.Triple));
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	[Fact]
	public void HasAnyVisibleBorder_BarVisible_ReturnsTrue()
	{
		var borders = new ParagraphBorders(Bar: new ParagraphBorder(BorderStyle.Thick));
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	// ===================================================================
	// GetBorder
	// ===================================================================

	[Fact]
	public void GetBorder_Top_ReturnsTopBorder()
	{
		var top = new ParagraphBorder(BorderStyle.Single, 8, 1f, "FF0000");
		var borders = new ParagraphBorders(Top: top);

		borders.GetBorder(BorderEdge.Top).Should().Be(top);
	}

	[Fact]
	public void GetBorder_Bottom_ReturnsBottomBorder()
	{
		var bottom = new ParagraphBorder(BorderStyle.Double, 12);
		var borders = new ParagraphBorders(Bottom: bottom);

		borders.GetBorder(BorderEdge.Bottom).Should().Be(bottom);
	}

	[Fact]
	public void GetBorder_Left_ReturnsLeftBorder()
	{
		var left = new ParagraphBorder(BorderStyle.Dotted);
		var borders = new ParagraphBorders(Left: left);

		borders.GetBorder(BorderEdge.Left).Should().Be(left);
	}

	[Fact]
	public void GetBorder_Right_ReturnsRightBorder()
	{
		var right = new ParagraphBorder(BorderStyle.Dashed);
		var borders = new ParagraphBorders(Right: right);

		borders.GetBorder(BorderEdge.Right).Should().Be(right);
	}

	[Fact]
	public void GetBorder_Between_ReturnsBetweenBorder()
	{
		var between = new ParagraphBorder(BorderStyle.Wave);
		var borders = new ParagraphBorders(Between: between);

		borders.GetBorder(BorderEdge.Between).Should().Be(between);
	}

	[Fact]
	public void GetBorder_Bar_ReturnsBarBorder()
	{
		var bar = new ParagraphBorder(BorderStyle.Thick, 16, 0f, "0000FF");
		var borders = new ParagraphBorders(Bar: bar);

		borders.GetBorder(BorderEdge.Bar).Should().Be(bar);
	}

	[Fact]
	public void GetBorder_Undefined_ReturnsNull()
	{
		var borders = new ParagraphBorders(Top: new ParagraphBorder(BorderStyle.Single));

		borders.GetBorder(BorderEdge.Bottom).Should().BeNull();
	}

	[Fact]
	public void GetBorder_UnknownEdge_ReturnsNull()
	{
		var borders = new ParagraphBorders(Top: new ParagraphBorder(BorderStyle.Single));

		borders.GetBorder((BorderEdge)999).Should().BeNull();
	}

	// ===================================================================
	// Record equality
	// ===================================================================

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new ParagraphBorders(
			Top: new ParagraphBorder(BorderStyle.Single, 8),
			Bottom: new ParagraphBorder(BorderStyle.Single, 8));
		var b = new ParagraphBorders(
			Top: new ParagraphBorder(BorderStyle.Single, 8),
			Bottom: new ParagraphBorder(BorderStyle.Single, 8));

		a.Should().Be(b);
	}

	[Fact]
	public void Equality_DifferentValues_NotEqual()
	{
		var a = new ParagraphBorders(Top: new ParagraphBorder(BorderStyle.Single));
		var b = new ParagraphBorders(Top: new ParagraphBorder(BorderStyle.Double));

		a.Should().NotBe(b);
	}
}
