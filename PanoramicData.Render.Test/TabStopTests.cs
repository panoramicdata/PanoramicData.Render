using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

public class TabStopTests
{
	// ===================================================================
	// TabStop record
	// ===================================================================

	[Fact]
	public void TabStop_Defaults()
	{
		var stop = new TabStop(1440f);

		stop.PositionTwips.Should().Be(1440f);
		stop.Type.Should().Be(TabStopType.Left);
		stop.Leader.Should().Be(TabStopLeader.None);
	}

	[Fact]
	public void TabStop_ExplicitValues()
	{
		var stop = new TabStop(2880f, TabStopType.Center, TabStopLeader.Dot);

		stop.PositionTwips.Should().Be(2880f);
		stop.Type.Should().Be(TabStopType.Center);
		stop.Leader.Should().Be(TabStopLeader.Dot);
	}

	[Fact]
	public void TabStop_Equality_SameValues()
	{
		var a = new TabStop(1440f, TabStopType.Right, TabStopLeader.Hyphen);
		var b = new TabStop(1440f, TabStopType.Right, TabStopLeader.Hyphen);
		a.Should().Be(b);
	}

	[Fact]
	public void TabStop_Equality_DifferentValues()
	{
		var a = new TabStop(1440f, TabStopType.Left);
		var b = new TabStop(1440f, TabStopType.Center);
		a.Should().NotBe(b);
	}

	// ===================================================================
	// TabStopProfile defaults
	// ===================================================================

	[Fact]
	public void Profile_Default_HasNoExplicitStops()
	{
		var profile = TabStopProfile.Default;

		profile.ExplicitStops.Should().BeEmpty();
		profile.DefaultIntervalTwips.Should().Be(720f);
	}

	[Fact]
	public void Profile_CustomInterval()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), 1440f);

		profile.DefaultIntervalTwips.Should().Be(1440f);
	}

	// ===================================================================
	// ResolveNextTabStop — explicit stops
	// ===================================================================

	[Fact]
	public void Resolve_FirstExplicitStop_AtOrigin()
	{
		var stops = new[] { new TabStop(720f), new TabStop(1440f) };
		var profile = new TabStopProfile(stops);

		var result = profile.ResolveNextTabStop(0f);

		result.PositionTwips.Should().Be(720f);
		result.Type.Should().Be(TabStopType.Left);
	}

	[Fact]
	public void Resolve_SkipsPastStops()
	{
		var stops = new[]
		{
			new TabStop(720f, TabStopType.Left),
			new TabStop(1440f, TabStopType.Center),
			new TabStop(2160f, TabStopType.Right)
		};
		var profile = new TabStopProfile(stops);

		var result = profile.ResolveNextTabStop(800f);

		result.PositionTwips.Should().Be(1440f);
		result.Type.Should().Be(TabStopType.Center);
	}

	[Fact]
	public void Resolve_ExactlyOnStop_AdvancesToNext()
	{
		var stops = new[]
		{
			new TabStop(720f),
			new TabStop(1440f, TabStopType.Right)
		};
		var profile = new TabStopProfile(stops);

		// Exactly on 720 — should advance to 1440
		var result = profile.ResolveNextTabStop(720f);

		result.PositionTwips.Should().Be(1440f);
	}

	[Fact]
	public void Resolve_PreservesLeader()
	{
		var stops = new[]
		{
			new TabStop(2880f, TabStopType.Right, TabStopLeader.Dot)
		};
		var profile = new TabStopProfile(stops);

		var result = profile.ResolveNextTabStop(0f);

		result.Leader.Should().Be(TabStopLeader.Dot);
	}

	[Fact]
	public void Resolve_PreservesDecimalType()
	{
		var stops = new[]
		{
			new TabStop(1440f, TabStopType.Decimal, TabStopLeader.None)
		};
		var profile = new TabStopProfile(stops);

		var result = profile.ResolveNextTabStop(0f);

		result.Type.Should().Be(TabStopType.Decimal);
	}

	[Fact]
	public void Resolve_PreservesBarType()
	{
		var stops = new[]
		{
			new TabStop(1440f, TabStopType.Bar)
		};
		var profile = new TabStopProfile(stops);

		var result = profile.ResolveNextTabStop(0f);

		result.Type.Should().Be(TabStopType.Bar);
	}

	// ===================================================================
	// ResolveNextTabStop — default (generated) stops
	// ===================================================================

	[Fact]
	public void Resolve_NoExplicitStops_UsesDefault()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), 720f);

		var result = profile.ResolveNextTabStop(0f);

		result.PositionTwips.Should().Be(720f);
		result.Type.Should().Be(TabStopType.Left);
		result.Leader.Should().Be(TabStopLeader.None);
	}

	[Fact]
	public void Resolve_BeyondExplicitStops_GeneratesDefaults()
	{
		var stops = new[] { new TabStop(720f) };
		var profile = new TabStopProfile(stops, 720f);

		// Beyond explicit stop at 720 → next default at 1440
		var result = profile.ResolveNextTabStop(800f);

		result.PositionTwips.Should().Be(1440f);
	}

	[Fact]
	public void Resolve_DefaultStops_Successive()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), 720f);

		var r1 = profile.ResolveNextTabStop(0f);
		r1.PositionTwips.Should().Be(720f);

		var r2 = profile.ResolveNextTabStop(720f);
		r2.PositionTwips.Should().Be(1440f);

		var r3 = profile.ResolveNextTabStop(1440f);
		r3.PositionTwips.Should().Be(2160f);
	}

	[Fact]
	public void Resolve_DefaultStops_CustomInterval()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), 1440f);

		var result = profile.ResolveNextTabStop(100f);

		result.PositionTwips.Should().Be(1440f);
	}

	[Fact]
	public void Resolve_DefaultStops_MidInterval()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), 720f);

		var result = profile.ResolveNextTabStop(1000f);

		result.PositionTwips.Should().Be(1440f); // 720*2
	}

	[Fact]
	public void Resolve_DefaultDisabled_ZeroInterval_MinimalAdvance()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), 0f);

		var result = profile.ResolveNextTabStop(500f);

		result.PositionTwips.Should().Be(501f); // Advances by 1 twip minimum
	}

	[Fact]
	public void Resolve_DefaultDisabled_NegativeInterval_MinimalAdvance()
	{
		var profile = new TabStopProfile(Array.Empty<TabStop>(), -100f);

		var result = profile.ResolveNextTabStop(500f);

		result.PositionTwips.Should().Be(501f);
	}
}
