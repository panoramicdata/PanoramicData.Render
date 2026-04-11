namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class VmlStyleParserTests
{
	[Fact]
	public void Parse_NullStyle_ReturnsEmptyDictionary()
	{
		var result = VmlStyleParser.Parse(null);

		result.Should().BeEmpty();
	}

	[Fact]
	public void Parse_EmptyStyle_ReturnsEmptyDictionary()
	{
		var result = VmlStyleParser.Parse("");

		result.Should().BeEmpty();
	}

	[Fact]
	public void Parse_SingleProperty_ReturnsSingleEntry()
	{
		var result = VmlStyleParser.Parse("position:absolute");

		result.Should().ContainSingle();
		result["position"].Should().Be("absolute");
	}

	[Fact]
	public void Parse_MultipleProperties_ReturnsAllEntries()
	{
		var result = VmlStyleParser.Parse("position:absolute;width:527.85pt;height:131.95pt");

		result.Should().HaveCount(3);
		result["position"].Should().Be("absolute");
		result["width"].Should().Be("527.85pt");
		result["height"].Should().Be("131.95pt");
	}

	[Fact]
	public void Parse_WithSpaces_TrimsKeysAndValues()
	{
		var result = VmlStyleParser.Parse(" position : absolute ; width : 100pt ");

		result["position"].Should().Be("absolute");
		result["width"].Should().Be("100pt");
	}

	[Fact]
	public void Parse_MsoProperties_ParsedCorrectly()
	{
		var result = VmlStyleParser.Parse("mso-position-horizontal:center;mso-position-vertical:center");

		result["mso-position-horizontal"].Should().Be("center");
		result["mso-position-vertical"].Should().Be("center");
	}

	[Fact]
	public void Parse_RotationProperty_ParsedCorrectly()
	{
		var result = VmlStyleParser.Parse("rotation:315");

		result["rotation"].Should().Be("315");
	}

	[Fact]
	public void Parse_IsCaseInsensitive()
	{
		var result = VmlStyleParser.Parse("Position:absolute");

		result["position"].Should().Be("absolute");
		result["POSITION"].Should().Be("absolute");
	}

	[Fact]
	public void Parse_EntryWithoutColon_Skipped()
	{
		var result = VmlStyleParser.Parse("invalidentry;position:absolute");

		result.Should().ContainSingle();
		result.Should().ContainKey("position");
	}

	[Fact]
	public void ParseDimensionToTwips_Null_ReturnsZero()
	{
		VmlStyleParser.ParseDimensionToTwips(null).Should().Be(0f);
	}

	[Fact]
	public void ParseDimensionToTwips_PointValue_ConvertsCorrectly()
	{
		var result = VmlStyleParser.ParseDimensionToTwips("72pt");

		// 72pt = 72 * 20 = 1440 twips (1 inch)
		result.Should().Be(1440f);
	}

	[Fact]
	public void ParseDimensionToTwips_FractionalPoints_ConvertsCorrectly()
	{
		var result = VmlStyleParser.ParseDimensionToTwips("527.85pt");

		result.Should().BeApproximately(527.85f * 20f, 0.01f);
	}

	[Fact]
	public void ParseDimensionToTwips_InchValue_ConvertsCorrectly()
	{
		var result = VmlStyleParser.ParseDimensionToTwips("1in");

		result.Should().Be(1440f);
	}

	[Fact]
	public void ParseDimensionToTwips_CmValue_ConvertsCorrectly()
	{
		var result = VmlStyleParser.ParseDimensionToTwips("2.54cm");

		result.Should().BeApproximately(1440f, 0.5f);
	}

	[Fact]
	public void ParseDimensionToTwips_MmValue_ConvertsCorrectly()
	{
		var result = VmlStyleParser.ParseDimensionToTwips("25.4mm");

		result.Should().BeApproximately(1440f, 0.5f);
	}

	[Fact]
	public void ParseDimensionToTwips_PxValue_ConvertsCorrectly()
	{
		var result = VmlStyleParser.ParseDimensionToTwips("96px");

		// 96px at 96 DPI = 1 inch = 1440 twips
		result.Should().Be(96f * 15f);
	}

	[Fact]
	public void ParseDimensionToTwips_UnknownUnit_ReturnsZero()
	{
		VmlStyleParser.ParseDimensionToTwips("100em").Should().Be(0f);
	}

	[Fact]
	public void ParseRotation_ValidDegrees_ReturnsValue()
	{
		VmlStyleParser.ParseRotation("315").Should().Be(315f);
	}

	[Fact]
	public void ParseRotation_NegativeDegrees_ReturnsValue()
	{
		VmlStyleParser.ParseRotation("-45").Should().Be(-45f);
	}

	[Fact]
	public void ParseRotation_Null_ReturnsZero()
	{
		VmlStyleParser.ParseRotation(null).Should().Be(0f);
	}

	[Fact]
	public void ParseRotation_InvalidValue_ReturnsZero()
	{
		VmlStyleParser.ParseRotation("abc").Should().Be(0f);
	}
}
