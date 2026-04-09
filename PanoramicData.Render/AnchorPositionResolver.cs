namespace PanoramicData.Render;

/// <summary>
/// Resolves absolute page coordinates for anchored images.
/// </summary>
internal static class AnchorPositionResolver
{
	/// <summary>
	/// Resolves the absolute top-left position of an anchored image in page twips.
	/// </summary>
	/// <param name="anchor">The anchored image element.</param>
	/// <param name="section">The page section geometry.</param>
	/// <param name="paragraphXTwips">The anchor paragraph X position from the page origin in twips.</param>
	/// <param name="paragraphYTwips">The anchor paragraph Y position from the page origin in twips.</param>
	/// <param name="paragraphWidthTwips">The anchor paragraph width in twips.</param>
	/// <returns>The resolved absolute page position in twips.</returns>
	public static AnchorAbsolutePosition ResolveAbsolutePosition(
		AnchorImageRunElement anchor,
		SectionInfo section,
		float paragraphXTwips,
		float paragraphYTwips,
		float paragraphWidthTwips)
	{
		ArgumentNullException.ThrowIfNull(anchor);
		ArgumentNullException.ThrowIfNull(section);

		var imageWidthTwips = TwipConverter.EmusToTwips(anchor.WidthEmu);
		var imageHeightTwips = TwipConverter.EmusToTwips(anchor.HeightEmu);

		var horizontalContext = ResolveHorizontalContext(anchor.HorizontalRelativeFrom, section, paragraphXTwips, paragraphWidthTwips);
		var verticalContext = ResolveVerticalContext(anchor.VerticalRelativeFrom, section, paragraphYTwips);

		var x = ResolveAlignedOffset(
			horizontalContext.Origin,
			horizontalContext.Length,
			imageWidthTwips,
			anchor.HorizontalAlignment,
			isHorizontal: true)
			+ TwipConverter.EmusToTwips(anchor.HorizontalOffsetEmu);

		var y = ResolveAlignedOffset(
			verticalContext.Origin,
			verticalContext.Length,
			imageHeightTwips,
			anchor.VerticalAlignment,
			isHorizontal: false)
			+ TwipConverter.EmusToTwips(anchor.VerticalOffsetEmu);

		return new AnchorAbsolutePosition(x, y);
	}

	private static (float Origin, float Length) ResolveHorizontalContext(
		AnchorRelativeFrom relativeFrom,
		SectionInfo section,
		float paragraphXTwips,
		float paragraphWidthTwips)
	{
		var marginLeft = section.MarginLeft;
		var marginRight = section.MarginRight;
		var contentWidth = section.PageWidth - marginLeft - marginRight;

		return relativeFrom switch
		{
			AnchorRelativeFrom.Page => (0f, section.PageWidth),
			AnchorRelativeFrom.Margin => (marginLeft, contentWidth),
			AnchorRelativeFrom.LeftMargin => (0f, marginLeft),
			AnchorRelativeFrom.RightMargin => (section.PageWidth - marginRight, marginRight),
			AnchorRelativeFrom.Column => (paragraphXTwips, paragraphWidthTwips),
			AnchorRelativeFrom.Character => (paragraphXTwips, paragraphWidthTwips),
			AnchorRelativeFrom.Paragraph => (paragraphXTwips, paragraphWidthTwips),
			AnchorRelativeFrom.InsideMargin => (marginLeft, contentWidth),
			AnchorRelativeFrom.OutsideMargin => (marginLeft, contentWidth),
			_ => (paragraphXTwips, paragraphWidthTwips)
		};
	}

	private static (float Origin, float Length) ResolveVerticalContext(
		AnchorRelativeFrom relativeFrom,
		SectionInfo section,
		float paragraphYTwips)
	{
		var marginTop = section.MarginTop;
		var marginBottom = section.MarginBottom;
		var contentHeight = section.PageHeight - marginTop - marginBottom;

		return relativeFrom switch
		{
			AnchorRelativeFrom.Page => (0f, section.PageHeight),
			AnchorRelativeFrom.Margin => (marginTop, contentHeight),
			AnchorRelativeFrom.TopMargin => (0f, marginTop),
			AnchorRelativeFrom.BottomMargin => (section.PageHeight - marginBottom, marginBottom),
			AnchorRelativeFrom.Paragraph => (paragraphYTwips, 0f),
			AnchorRelativeFrom.Line => (paragraphYTwips, 0f),
			AnchorRelativeFrom.InsideMargin => (marginTop, contentHeight),
			AnchorRelativeFrom.OutsideMargin => (marginTop, contentHeight),
			_ => (paragraphYTwips, 0f)
		};
	}

	private static float ResolveAlignedOffset(float origin, float referenceLength, float imageLength, AnchorAlignment alignment, bool isHorizontal)
	{
		if (alignment == AnchorAlignment.None)
		{
			return origin;
		}

		if (alignment == AnchorAlignment.Center)
		{
			return origin + ((referenceLength - imageLength) / 2f);
		}

		if (isHorizontal)
		{
			if (alignment is AnchorAlignment.Right or AnchorAlignment.Outside)
			{
				return origin + (referenceLength - imageLength);
			}

			return origin;
		}

		if (alignment == AnchorAlignment.Bottom)
		{
			return origin + (referenceLength - imageLength);
		}

		return origin;
	}
}

/// <summary>
/// Represents an anchored image absolute page position in twips.
/// </summary>
/// <param name="X">Absolute X position from page left in twips.</param>
/// <param name="Y">Absolute Y position from page top in twips.</param>
internal readonly record struct AnchorAbsolutePosition(float X, float Y);
