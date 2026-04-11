namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class TabStopParserTests
{
	[Fact]
	public void ParseTabStops_NullProperties_ReturnsEmptyProfile()
	{
		var profile = TabStopParser.ParseTabStops(null);

		profile.ExplicitStops.Should().BeEmpty();
		profile.DefaultIntervalTwips.Should().Be(720f);
	}

	[Fact]
	public void ParseTabStops_NoTabs_ReturnsEmptyProfile()
	{
		var pPr = new ParagraphProperties();

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().BeEmpty();
	}

	[Fact]
	public void ParseTabStops_LeftTab_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Left, Position = 2880 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().ContainSingle();
		profile.ExplicitStops[0].PositionTwips.Should().Be(2880f);
		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Left);
		profile.ExplicitStops[0].Leader.Should().Be(TabStopLeader.None);
	}

	[Fact]
	public void ParseTabStops_CenterTab_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Center, Position = 4320 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().ContainSingle();
		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Center);
	}

	[Fact]
	public void ParseTabStops_RightTab_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Right, Position = 9360 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().ContainSingle();
		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Right);
	}

	[Fact]
	public void ParseTabStops_DecimalTab_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Decimal, Position = 5760 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().ContainSingle();
		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Decimal);
	}

	[Fact]
	public void ParseTabStops_BarTab_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Bar, Position = 1440 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().ContainSingle();
		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Bar);
		profile.ExplicitStops[0].PositionTwips.Should().Be(1440f);
	}

	[Fact]
	public void ParseTabStops_ClearTab_IsSkipped()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Clear, Position = 2880 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().BeEmpty();
	}

	[Fact]
	public void ParseTabStops_MultipleTabs_ParsedInOrder()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Left, Position = 1440 },
				new TabStop { Val = TabStopValues.Bar, Position = 2880 },
				new TabStop { Val = TabStopValues.Right, Position = 9360 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops.Should().HaveCount(3);
		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Left);
		profile.ExplicitStops[1].Type.Should().Be(TabStopType.Bar);
		profile.ExplicitStops[2].Type.Should().Be(TabStopType.Right);
	}

	[Fact]
	public void ParseTabStops_DotLeader_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Right, Position = 9360, Leader = TabStopLeaderCharValues.Dot }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Leader.Should().Be(TabStopLeader.Dot);
	}

	[Fact]
	public void ParseTabStops_HyphenLeader_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Right, Position = 9360, Leader = TabStopLeaderCharValues.Hyphen }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Leader.Should().Be(TabStopLeader.Hyphen);
	}

	[Fact]
	public void ParseTabStops_HeavyLeader_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Right, Position = 9360, Leader = TabStopLeaderCharValues.Heavy }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Leader.Should().Be(TabStopLeader.Heavy);
	}

	[Fact]
	public void ParseTabStops_UnderscoreLeader_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Right, Position = 9360, Leader = TabStopLeaderCharValues.Underscore }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Leader.Should().Be(TabStopLeader.Underscore);
	}

	[Fact]
	public void ParseTabStops_MiddleDotLeader_MapsCorrectly()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Right, Position = 9360, Leader = TabStopLeaderCharValues.MiddleDot }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Leader.Should().Be(TabStopLeader.MiddleDot);
	}

	[Fact]
	public void ParseTabStops_StartValue_MapsToLeft()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Start, Position = 1440 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Left);
	}

	[Fact]
	public void ParseTabStops_EndValue_MapsToRight()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.End, Position = 9360 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Right);
	}

	[Fact]
	public void ParseTabStops_NumberValue_MapsToDecimal()
	{
		var pPr = new ParagraphProperties(
			new Tabs(
				new TabStop { Val = TabStopValues.Number, Position = 5760 }
			));

		var profile = TabStopParser.ParseTabStops(pPr);

		profile.ExplicitStops[0].Type.Should().Be(TabStopType.Decimal);
	}
}
