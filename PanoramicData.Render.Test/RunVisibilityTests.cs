using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class RunVisibilityTests
{
	[Fact]
	public void NotVanished_IsVisible()
	{
		RunVisibility.IsVisible(vanish: false, showHiddenText: false)
			.Should().BeTrue();
	}

	[Fact]
	public void NotVanished_ShowHiddenTrue_StillVisible()
	{
		RunVisibility.IsVisible(vanish: false, showHiddenText: true)
			.Should().BeTrue();
	}

	[Fact]
	public void Vanished_ShowHiddenFalse_IsNotVisible()
	{
		RunVisibility.IsVisible(vanish: true, showHiddenText: false)
			.Should().BeFalse();
	}

	[Fact]
	public void Vanished_ShowHiddenTrue_IsVisible()
	{
		RunVisibility.IsVisible(vanish: true, showHiddenText: true)
			.Should().BeTrue();
	}
}
