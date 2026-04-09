namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class LayeredWrapLayoutEngineTests
{
	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullSegment()
	{
		var segments = LayeredWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, []);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_WithBehindAndFrontRegions_DoesNotDisplaceText()
	{
		var regions = new[]
		{
			new FloatingLayeredRegion(100f, 900f, 200f, 200f, BehindDocument: true),
			new FloatingLayeredRegion(200f, 950f, 250f, 250f, BehindDocument: false)
		};

		var segments = LayeredWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, regions);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_NullRegions_ThrowsArgumentNullException()
	{
		var act = () => LayeredWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveContentWidth_ThrowsArgumentOutOfRangeException()
	{
		var act = () => LayeredWrapLayoutEngine.ComputeAvailableSegments(100f, 0f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
