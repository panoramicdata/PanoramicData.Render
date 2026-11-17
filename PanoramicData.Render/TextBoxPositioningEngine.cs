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
	/// <param name="paragraph">The anchor paragraph position and width from the page origin in twips.</param>
	/// <param name="font">Optional fallback font settings used to lay out text.</param>
	/// <returns>The positioned text box layout.</returns>
	public static PositionedTextBoxLayout Position(
		ShapeTextFrameInfo textFrame,
		AnchorPlacementInfo anchorPlacement,
		long widthEmu,
		long heightEmu,
		SectionInfo section,
		ParagraphBox paragraph,
		TextBoxFont? font = null)
	{
		ArgumentNullException.ThrowIfNull(textFrame);
		ArgumentNullException.ThrowIfNull(anchorPlacement);
		ArgumentNullException.ThrowIfNull(section);
		var effectiveFont = font ?? new TextBoxFont();
		ValidateSize(widthEmu, heightEmu);

		var widthTwips = TwipConverter.EmusToTwips(widthEmu);
		var heightTwips = TwipConverter.EmusToTwips(heightEmu);
		var leftInsetTwips = TwipConverter.EmusToTwips(textFrame.LeftInsetEmu);
		var topInsetTwips = TwipConverter.EmusToTwips(textFrame.TopInsetEmu);
		var rightInsetTwips = TwipConverter.EmusToTwips(textFrame.RightInsetEmu);
		var bottomInsetTwips = TwipConverter.EmusToTwips(textFrame.BottomInsetEmu);
		var position = AnchorPositionResolver.ResolveAbsolutePosition(anchorPlacement, widthEmu, heightEmu, section, paragraph.XTwips, paragraph.YTwips, paragraph.WidthTwips);
		var (blocks, contentHeightTwips) = TextBoxLayoutEngine.Layout(textFrame, widthTwips, effectiveFont.FontFamily, effectiveFont.FontSizePoints, effectiveFont.FontResolver);
		var contentWidthTwips = MathF.Max(0f, widthTwips - leftInsetTwips - rightInsetTwips);
		var (effectiveHeightTwips, contentBoxHeightTwips) = ResolveHeights(
			textFrame,
			heightTwips,
			contentHeightTwips,
			topInsetTwips,
			bottomInsetTwips);

		return new PositionedTextBoxLayout(
			XTwips: position.X,
			YTwips: position.Y,
			WidthTwips: widthTwips,
			HeightTwips: effectiveHeightTwips,
			ContentXTwips: position.X + leftInsetTwips,
			ContentYTwips: position.Y + topInsetTwips,
			ContentWidthTwips: contentWidthTwips,
			ContentBoxHeightTwips: contentBoxHeightTwips,
			Blocks: blocks,
			ContentHeightTwips: contentHeightTwips,
			AnchorPlacement: anchorPlacement);
	}

	private static void ValidateSize(long widthEmu, long heightEmu)
	{
		if (widthEmu <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(widthEmu));
		}

		if (heightEmu <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(heightEmu));
		}
	}

	/// <summary>
	/// Registers the positioned text box as a wrap exclusion region.
	/// </summary>
	/// <param name="layout">The positioned text box layout.</param>
	/// <param name="registry">The wrap registry to receive exclusion regions.</param>
	public static void RegisterWrapRegion(PositionedTextBoxLayout layout, WrapRegionRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(registry);

		if (layout.AnchorPlacement.BehindDocument || layout.AnchorPlacement.WrapStyle == AnchorWrapStyle.None)
		{
			return;
		}

		var distanceTopTwips = TwipConverter.EmusToTwips(layout.AnchorPlacement.DistanceTopEmu);
		var distanceBottomTwips = TwipConverter.EmusToTwips(layout.AnchorPlacement.DistanceBottomEmu);
		var distanceLeftTwips = TwipConverter.EmusToTwips(layout.AnchorPlacement.DistanceLeftEmu);
		var distanceRightTwips = TwipConverter.EmusToTwips(layout.AnchorPlacement.DistanceRightEmu);

		var wrapStyle = layout.AnchorPlacement.WrapStyle;
		if (wrapStyle == AnchorWrapStyle.Square)
		{
			registry.AddSquareRegion(new FloatingSquareWrapRegion(
				layout.XTwips,
				layout.YTwips,
				layout.WidthTwips,
				layout.HeightTwips,
				distanceTopTwips,
				distanceBottomTwips,
				distanceLeftTwips,
				distanceRightTwips));
		}
		else if (wrapStyle == AnchorWrapStyle.TopAndBottom)
		{
			registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(
				layout.YTwips,
				layout.HeightTwips,
				distanceTopTwips,
				distanceBottomTwips));
		}
		else if (wrapStyle == AnchorWrapStyle.Tight)
		{
			registry.AddTightRegion(new FloatingTightWrapRegion(
				CreateRectanglePolygon(layout),
				distanceTopTwips,
				distanceBottomTwips,
				distanceLeftTwips,
				distanceRightTwips));
		}
	}

	private static (float OuterHeightTwips, float ContentBoxHeightTwips) ResolveHeights(
		ShapeTextFrameInfo textFrame,
		float heightTwips,
		float contentHeightTwips,
		float topInsetTwips,
		float bottomInsetTwips)
	{
		var contentBoxHeightTwips = MathF.Max(0f, heightTwips - topInsetTwips - bottomInsetTwips);
		if (textFrame.AutoFitMode != ShapeTextAutoFitMode.ShapeAutoFit)
		{
			return (heightTwips, contentBoxHeightTwips);
		}

		var requiredOuterHeightTwips = contentHeightTwips + topInsetTwips + bottomInsetTwips;
		var effectiveOuterHeightTwips = MathF.Max(heightTwips, requiredOuterHeightTwips);
		var effectiveContentBoxHeightTwips = MathF.Max(contentBoxHeightTwips, contentHeightTwips);
		return (effectiveOuterHeightTwips, effectiveContentBoxHeightTwips);
	}

	private static IReadOnlyList<TightWrapPoint> CreateRectanglePolygon(PositionedTextBoxLayout layout)
	{
		return
		[
			new TightWrapPoint(layout.XTwips, layout.YTwips),
			new TightWrapPoint(layout.XTwips + layout.WidthTwips, layout.YTwips),
			new TightWrapPoint(layout.XTwips + layout.WidthTwips, layout.YTwips + layout.HeightTwips),
			new TightWrapPoint(layout.XTwips, layout.YTwips + layout.HeightTwips)
		];
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
/// <summary>
/// The anchor paragraph position and width in twips.
/// </summary>
internal readonly record struct ParagraphBox(float XTwips, float YTwips, float WidthTwips);

/// <summary>
/// Fallback font settings used to lay out text box content.
/// </summary>
internal sealed record TextBoxFont(string FontFamily = "Times New Roman", float FontSizePoints = TextBoxLayoutEngine.DefaultFontSizePoints, FontResolver? FontResolver = null);
