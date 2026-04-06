namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses and applies OOXML toggle property semantics.
/// </summary>
internal static class TogglePropertyLogic
{
	/// <summary>
	/// Parses supported toggle instructions from style run properties.
	/// </summary>
	/// <param name="runProperties">The style run properties element.</param>
	/// <returns>Parsed toggle instructions.</returns>
	public static ToggleProperties Parse(StyleRunProperties? runProperties)
	{
		if (runProperties is null)
		{
			return new ToggleProperties();
		}

		return new ToggleProperties
		{
			Bold = ParseInstruction(runProperties.GetFirstChild<Bold>()),
			Italic = ParseInstruction(runProperties.GetFirstChild<Italic>()),
			Caps = ParseInstruction(runProperties.GetFirstChild<Caps>()),
			SmallCaps = ParseInstruction(runProperties.GetFirstChild<SmallCaps>()),
			Strike = ParseInstruction(runProperties.GetFirstChild<Strike>()),
			DoubleStrike = ParseInstruction(runProperties.GetFirstChild<DoubleStrike>()),
			Vanish = ParseInstruction(runProperties.GetFirstChild<Vanish>()),
			Emboss = ParseInstruction(runProperties.GetFirstChild<Emboss>()),
			Imprint = ParseInstruction(runProperties.GetFirstChild<Imprint>()),
			Outline = ParseInstruction(runProperties.GetFirstChild<Outline>()),
			Shadow = ParseInstruction(runProperties.GetFirstChild<Shadow>())
		};
	}

	/// <summary>
	/// Applies a toggle instruction to an inherited value.
	/// </summary>
	/// <param name="inheritedValue">The inherited value.</param>
	/// <param name="instruction">The parsed toggle instruction.</param>
	/// <returns>The resolved value.</returns>
	public static bool Apply(bool inheritedValue, ToggleInstruction instruction)
	{
		if (instruction == ToggleInstruction.Toggle)
		{
			return !inheritedValue;
		}

		if (instruction == ToggleInstruction.SetFalse)
		{
			return false;
		}

		return inheritedValue;
	}

	/// <summary>
	/// Applies all toggle instructions to an inherited toggle state.
	/// </summary>
	/// <param name="inheritedState">The inherited state.</param>
	/// <param name="toggles">The toggle instructions to apply.</param>
	/// <returns>The resolved toggle state.</returns>
	public static ToggleState Apply(ToggleState inheritedState, ToggleProperties toggles)
	{
		ArgumentNullException.ThrowIfNull(inheritedState);
		ArgumentNullException.ThrowIfNull(toggles);

		return new ToggleState
		{
			Bold = Apply(inheritedState.Bold, toggles.Bold),
			Italic = Apply(inheritedState.Italic, toggles.Italic),
			Caps = Apply(inheritedState.Caps, toggles.Caps),
			SmallCaps = Apply(inheritedState.SmallCaps, toggles.SmallCaps),
			Strike = Apply(inheritedState.Strike, toggles.Strike),
			DoubleStrike = Apply(inheritedState.DoubleStrike, toggles.DoubleStrike),
			Vanish = Apply(inheritedState.Vanish, toggles.Vanish),
			Emboss = Apply(inheritedState.Emboss, toggles.Emboss),
			Imprint = Apply(inheritedState.Imprint, toggles.Imprint),
			Outline = Apply(inheritedState.Outline, toggles.Outline),
			Shadow = Apply(inheritedState.Shadow, toggles.Shadow)
		};
	}

	private static ToggleInstruction ParseInstruction(OnOffType? property)
	{
		if (property is null)
		{
			return ToggleInstruction.None;
		}

		var val = property.Val?.Value;
		if (val is null || val.Value)
		{
			return ToggleInstruction.Toggle;
		}

		return ToggleInstruction.SetFalse;
	}
}
