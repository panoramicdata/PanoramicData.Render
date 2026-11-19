namespace PanoramicData.Render.Test;

using AwesomeAssertions;

internal static class WrapAssert
{
	public static void Segments(IReadOnlyList<WrapLineSegment> actual, params (float X, float Width)[] expected)
		=> Segments(actual, 0f, expected);

	public static void Segments(IReadOnlyList<WrapLineSegment> actual, float tolerance, params (float X, float Width)[] expected)
	{
		actual.Should().HaveCount(expected.Length);
		for (var i = 0; i < expected.Length; i++)
		{
			actual[i].XTwips.Should().BeApproximately(expected[i].X, tolerance);
			actual[i].WidthTwips.Should().BeApproximately(expected[i].Width, tolerance);
		}
	}

	public static void Guard<TRegion>(
		string invalidArgument,
		Func<float, float, float, float, IReadOnlyList<TRegion>, IReadOnlyList<WrapLineSegment>> compute)
	{
		switch (invalidArgument)
		{
			case "regions":
				var nullRegions = () => compute(100f, 500f, 1000f, 100f, null!);
				nullRegions.Should().Throw<ArgumentNullException>();
				break;
			case "width":
				var zeroWidth = () => compute(100f, 0f, 1000f, 100f, []);
				zeroWidth.Should().Throw<ArgumentOutOfRangeException>();
				break;
			default:
				var zeroHeight = () => compute(100f, 500f, 1000f, 0f, []);
				zeroHeight.Should().Throw<ArgumentOutOfRangeException>();
				break;
		}
	}
}
