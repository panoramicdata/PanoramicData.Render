namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class TogglePropertyLogicTests
{
	[Fact]
	public void Parse_WithNullRunProperties_ReturnsNoneForAll()
	{
		var toggles = TogglePropertyLogic.Parse(null);

		toggles.Bold.Should().Be(ToggleInstruction.None);
		toggles.Italic.Should().Be(ToggleInstruction.None);
		toggles.Caps.Should().Be(ToggleInstruction.None);
		toggles.SmallCaps.Should().Be(ToggleInstruction.None);
		toggles.Strike.Should().Be(ToggleInstruction.None);
		toggles.DoubleStrike.Should().Be(ToggleInstruction.None);
		toggles.Vanish.Should().Be(ToggleInstruction.None);
		toggles.Emboss.Should().Be(ToggleInstruction.None);
		toggles.Imprint.Should().Be(ToggleInstruction.None);
		toggles.Outline.Should().Be(ToggleInstruction.None);
		toggles.Shadow.Should().Be(ToggleInstruction.None);
	}

	[Fact]
	public void Parse_WithOnOffElementWithoutVal_ReturnsToggleInstruction()
	{
		var toggles = TogglePropertyLogic.Parse(new StyleRunProperties(new Bold()));

		toggles.Bold.Should().Be(ToggleInstruction.Toggle);
	}

	[Fact]
	public void Parse_WithOnOffElementValTrue_ReturnsToggleInstruction()
	{
		var toggles = TogglePropertyLogic.Parse(new StyleRunProperties(new Bold { Val = true }));

		toggles.Bold.Should().Be(ToggleInstruction.Toggle);
	}

	[Fact]
	public void Parse_WithOnOffElementValFalse_ReturnsSetFalseInstruction()
	{
		var toggles = TogglePropertyLogic.Parse(new StyleRunProperties(new Bold { Val = false }));

		toggles.Bold.Should().Be(ToggleInstruction.SetFalse);
	}

	[Fact]
	public void Parse_WithAllSupportedProperties_MapsEachToggleInstruction()
	{
		var toggles = TogglePropertyLogic.Parse(new StyleRunProperties(
			new Bold(),
			new Italic { Val = false },
			new Caps(),
			new SmallCaps { Val = false },
			new Strike(),
			new DoubleStrike { Val = false },
			new Vanish(),
			new Emboss { Val = false },
			new Imprint(),
			new Outline { Val = false },
			new Shadow()));

		toggles.Bold.Should().Be(ToggleInstruction.Toggle);
		toggles.Italic.Should().Be(ToggleInstruction.SetFalse);
		toggles.Caps.Should().Be(ToggleInstruction.Toggle);
		toggles.SmallCaps.Should().Be(ToggleInstruction.SetFalse);
		toggles.Strike.Should().Be(ToggleInstruction.Toggle);
		toggles.DoubleStrike.Should().Be(ToggleInstruction.SetFalse);
		toggles.Vanish.Should().Be(ToggleInstruction.Toggle);
		toggles.Emboss.Should().Be(ToggleInstruction.SetFalse);
		toggles.Imprint.Should().Be(ToggleInstruction.Toggle);
		toggles.Outline.Should().Be(ToggleInstruction.SetFalse);
		toggles.Shadow.Should().Be(ToggleInstruction.Toggle);
	}

	[Theory]
	[InlineData(true, 0, true)]
	[InlineData(false, 0, false)]
	[InlineData(true, 1, false)]
	[InlineData(false, 1, true)]
	[InlineData(true, 2, false)]
	[InlineData(false, 2, false)]
	public void Apply_ResolvesSingleInstruction(bool inherited, int instructionCode, bool expected)
	{
		var instruction = (ToggleInstruction)instructionCode;
		var result = TogglePropertyLogic.Apply(inherited, instruction);

		result.Should().Be(expected);
	}

	[Fact]
	public void Apply_WithStateAndToggles_ResolvesAllProperties()
	{
		var state = new ToggleState
		{
			Bold = true,
			Italic = false,
			Caps = false,
			SmallCaps = true,
			Strike = false,
			DoubleStrike = true,
			Vanish = false,
			Emboss = true,
			Imprint = false,
			Outline = true,
			Shadow = false
		};

		var toggles = new ToggleProperties
		{
			Bold = ToggleInstruction.Toggle,
			Italic = ToggleInstruction.SetFalse,
			Caps = ToggleInstruction.Toggle,
			SmallCaps = ToggleInstruction.None,
			Strike = ToggleInstruction.Toggle,
			DoubleStrike = ToggleInstruction.SetFalse,
			Vanish = ToggleInstruction.None,
			Emboss = ToggleInstruction.Toggle,
			Imprint = ToggleInstruction.Toggle,
			Outline = ToggleInstruction.SetFalse,
			Shadow = ToggleInstruction.Toggle
		};

		var result = TogglePropertyLogic.Apply(state, toggles);

		result.Bold.Should().BeFalse();
		result.Italic.Should().BeFalse();
		result.Caps.Should().BeTrue();
		result.SmallCaps.Should().BeTrue();
		result.Strike.Should().BeTrue();
		result.DoubleStrike.Should().BeFalse();
		result.Vanish.Should().BeFalse();
		result.Emboss.Should().BeFalse();
		result.Imprint.Should().BeTrue();
		result.Outline.Should().BeFalse();
		result.Shadow.Should().BeTrue();
	}

	[Fact]
	public void Apply_WithNullState_ThrowsArgumentNullException()
	{
		var toggles = new ToggleProperties();
		var act = () => TogglePropertyLogic.Apply(null!, toggles);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Apply_WithNullToggles_ThrowsArgumentNullException()
	{
		var state = new ToggleState();
		var act = () => TogglePropertyLogic.Apply(state, null!);

		act.Should().Throw<ArgumentNullException>();
	}
}
