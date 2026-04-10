namespace PanoramicData.Render;

/// <summary>
/// Computes the absolute position and laid-out content for a floating text box.
/// </summary>
internal static class TextBoxPositioningEngine
{
	/// <summary>
	/// Positions a floating text box from shared anchor placement metadata.
	/// </summary>
	/// <param name="textFrame">The parsed text frame content.</param>
	/// <param name="anchorPlacement">The floating anchor placement metadata.</param>
	/// <param name="widthEmu">The text box width in EMUs.</param>
	/// <param name="heightEmu">The text box height in EMUs.</param>
	/// <param name="section">The page section geometry.</param>
	/// <param name="paragraphXTwips">The anchor paragraph X position from the page origin in twips.</param>
	/// <param name="paragraphYTwips">The anchor paragraph Y position from the page origin in twips.</param>
	/// <param name="paragraphWidthTwips">The anchor paragraph width in twips.</param>
	/// <param name="fontFamily">Fallback font family used to lay out text.</param>
	/// <param name="fontSizePoints">Fallback font size used to lay out text.</param>
	/// <returns>The positioned text box layout.</returns>
	public static PositionedTextBoxLayout Position(
		ShapeTextFrameInfo textFrame,
		AnchorPlacementInfo anchorPlacement,
		long widthEmu,
		long heightEmu,
		SectionInfo section,
		float paragraphXTwips,
		float paragraphYTwips,
		float paragraphWidthTwips,
		string fontFamily = "Times New Roman",
		float fontSizePoints = TextBoxLayoutEngine.DefaultFontSizePoints)
	{
		ArgumentNullException.ThrowIfNull(textFrame);
		ArgumentNullException.ThrowIfNull(anchorPlacement);
		ArgumentNullException.ThrowIfNull(section);

		if (widthEmu <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(widthEmu));
		}

		if (heightEmu <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(heightEmu));
		}

		var widthTwips = TwipConverter.EmusToTwips(widthEmu);
		var heightTwips = TwipConverter.EmusToTwips(heightEmu);
		var leftInsetTwips = TwipConverter.EmusToTwips(textFrame.LeftInsetEmu);
		var topInsetTwips = TwipConverter.EmusToTwips(textFrame.TopInsetEmu);
		var rightInsetTwips = TwipConverter.EmusToTwips(textFrame.RightInsetEmu);
		var bottomInsetTwips = TwipConverter.EmusToTwips(textFrame.BottomInsetEmu);
		var position = AnchorPositionResolver.ResolveAbsolutePosition(
			anchorPlacement,
			widthEmu,
			heightEmu,
			section,
			paragraphXTwips,
			paragraphYTwips,
			paragraphWidthTwips);
		var (blocks, contentHeightTwips) = TextBoxLayoutEngine.Layout(textFrame, widthTwips, fontFamily, fontSizePoints);
		var contentWidthTwips = MathF.Max(0f, widthTwips - leftInsetTwips - rightInsetTwips);
		var contentBoxHeightTwips = MathF.Max(0f, heightTwips - topInsetTwips - bottomInsetTwips);

		return new PositionedTextBoxLayout(
			XTwips: position.X,
			YTwips: position.Y,
			WidthTwips: widthTwips,
			HeightTwips: heightTwips,
			ContentXTwips: position.X + leftInsetTwips,
			ContentYTwips: position.Y + topInsetTwips,
			ContentWidthTwips: contentWidthTwips,
			ContentBoxHeightTwips: contentBoxHeightTwips,
			Blocks: blocks,
			ContentHeightTwips: contentHeightTwips,
			AnchorPlacement: anchorPlacement);
	}
}

/// <summary>
/// Represents a laid-out floating text box positioned on the page.
/// </summary>
/// <param name="XTwips">Absolute X position from page left in twips.</param>
/// <param name="YTwips">Absolute Y position from page top in twips.</param>
/// <param name="WidthTwips">Outer text box width in twips.</param>
/// <param name="HeightTwips">Outer text box height in twips.</param>
/// <param name="ContentXTwips">Absolute X position of the inner content box in twips.</param>
/// <param name="ContentYTwips">Absolute Y position of the inner content box in twips.</param>
/// <param name="ContentWidthTwips">Width of the inner content box in twips.</param>
/// <param name="ContentBoxHeightTwips">Height of the inner content box in twips.</param>
/// <param name="Blocks">Laid-out text box content blocks.</param>
/// <param name="ContentHeightTwips">Measured height of the laid-out content in twips.</param>
/// <param name="AnchorPlacement">The originating anchor placement metadata.</param>
internal readonly record struct PositionedTextBoxLayout(
	float XTwips,
	float YTwips,
	float WidthTwips,
	float HeightTwips,
	float ContentXTwips,
	float ContentYTwips,
	float ContentWidthTwips,
	float ContentBoxHeightTwips,
	IReadOnlyList<LayoutBlock> Blocks,
	float ContentHeightTwips,
	AnchorPlacementInfo AnchorPlacement);