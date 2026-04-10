namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Parses OpenXML run elements into <see cref="RunElement"/> instances.
/// </summary>
internal static class RunElementParser
{
	/// <summary>
	/// Parses the child elements of a run into an ordered list of <see cref="RunElement"/> instances.
	/// </summary>
	/// <param name="run">The OpenXML run element.</param>
	/// <returns>An ordered list of run elements.</returns>
	public static IReadOnlyList<RunElement> Parse(Run run)
	{
		ArgumentNullException.ThrowIfNull(run);

		var elements = new List<RunElement>();

		foreach (var child in run.ChildElements)
		{
			switch (child)
			{
				case Text text:
					elements.Add(new TextRunElement { Text = text.InnerText });
					break;

				case Break br:
					elements.Add(new BreakRunElement { BreakType = ParseBreakType(br) });
					break;

				case TabChar:
					elements.Add(new TabRunElement());
					break;

				case NoBreakHyphen:
					elements.Add(new NonBreakingHyphenRunElement());
					break;

				case Drawing drawing:
					ParseDrawing(drawing, elements);
					break;

				case EmbeddedObject oleObj:
					ParseOleObject(oleObj, elements);
					break;

				case FootnoteReference fnRef:
					elements.Add(new FootnoteReferenceRunElement
					{
						FootnoteId = fnRef.Id is null ? 0 : checked((int)fnRef.Id.Value)
					});
					break;

				case EndnoteReference enRef:
					elements.Add(new EndnoteReferenceRunElement
					{
						EndnoteId = enRef.Id is null ? 0 : checked((int)enRef.Id.Value)
					});
					break;
			}
		}

		return elements;
	}

	/// <summary>
	/// Parses a run into a <see cref="ParsedRun"/> containing the style ID and content elements.
	/// </summary>
	/// <param name="run">The OpenXML run element.</param>
	/// <returns>A <see cref="ParsedRun"/> with style and elements.</returns>
	public static ParsedRun ParseRun(Run run)
	{
		ArgumentNullException.ThrowIfNull(run);

		return new ParsedRun
		{
			StyleId = run.RunProperties?.RunStyle?.Val?.Value,
			Elements = Parse(run)
		};
	}

	/// <summary>
	/// Parses all runs within a paragraph.
	/// </summary>
	/// <param name="paragraph">The OpenXML paragraph element.</param>
	/// <returns>An ordered list of <see cref="ParsedRun"/> instances.</returns>
	public static IReadOnlyList<ParsedRun> ParseParagraphRuns(Paragraph paragraph)
	{
		ArgumentNullException.ThrowIfNull(paragraph);

		var runs = new List<ParsedRun>();
		foreach (var run in paragraph.Elements<Run>())
		{
			runs.Add(ParseRun(run));
		}

		return runs;
	}

	private static RunBreakType ParseBreakType(Break br)
	{
		if (br.Type is null)
		{
			return RunBreakType.Line;
		}

		if (br.Type.Value == BreakValues.Page)
		{
			return RunBreakType.Page;
		}

		if (br.Type.Value == BreakValues.Column)
		{
			return RunBreakType.Column;
		}

		return RunBreakType.Line;
	}

	private static void ParseOleObject(EmbeddedObject oleObj, List<RunElement> elements)
	{
		// Extract OLE relationship ID from o:OLEObject child.
		var oleRelId = oleObj.Descendants()
			.FirstOrDefault(e => e.LocalName == "OLEObject")
			?.GetAttributes()
			.FirstOrDefault(a => a.LocalName == "id")
			.Value ?? string.Empty;

		// Extract preview image relationship ID from v:imagedata child (local-name match avoids type ambiguity).
		var imageDataRelId = oleObj.Descendants()
			.FirstOrDefault(e => e.LocalName == "imagedata")
			?.GetAttributes()
			.FirstOrDefault(a => a.LocalName == "id")
			.Value ?? string.Empty;

		// Read original size from w:object attributes dxaOrig/dyaOrig (twips → EMU: × 914400/1440).
		const long DefaultEmu = 1905000L; // ~2 inches fallback
		var dxaOrig = oleObj.GetAttributes().FirstOrDefault(a => a.LocalName == "dxaOrig").Value;
		var dyaOrig = oleObj.GetAttributes().FirstOrDefault(a => a.LocalName == "dyaOrig").Value;
		var widthEmu = TryParseTwipsToEmu(dxaOrig) ?? DefaultEmu;
		var heightEmu = TryParseTwipsToEmu(dyaOrig) ?? DefaultEmu;

		elements.Add(new OleObjectRunElement
		{
			RelationshipId = oleRelId,
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			PreviewImageRelationshipId = imageDataRelId
		});
	}

	private static long? TryParseTwipsToEmu(string? twipsStr)
	{
		if (!long.TryParse(twipsStr, out var twips))
		{
			return null;
		}

		// Twips → EMU: twips / 1440 * 914400 = twips × 635
		return twips * 635L;
	}

	private static void ParseDrawing(Drawing drawing, List<RunElement> elements)
	{
		var inline = drawing.GetFirstChild<DW.Inline>();
		if (inline is not null)
		{
			var extent = inline.Extent;
			var inlineShapeProperties = inline.Descendants<A.ShapeProperties>().FirstOrDefault();
			var inlineTextFrame = ShapeTextFrameParser.Parse(inline);
			var inlineTransform = ShapeTransformParser.Parse(inlineShapeProperties);

			// Check for grouped shapes (wpg:wgp) first.
			var inlineWgp = inline.Descendants().FirstOrDefault(e => e.LocalName == "wgp");
			if (inlineWgp is not null)
			{
				elements.Add(GroupShapeParser.Parse(inlineWgp, extent?.Cx ?? 0, extent?.Cy ?? 0));
				return;
			}

			// Check for DrawingML shape (a:prstGeom) before image blip.
			var presetGeom = inline.Descendants<A.PresetGeometry>().FirstOrDefault();
			if (presetGeom is not null)
			{
				elements.Add(ParseDrawingShape(extent?.Cx ?? 0, extent?.Cy ?? 0, presetGeom, inlineShapeProperties, inlineTextFrame, inlineTransform));
				return;
			}

			var customGeom = inline.Descendants().FirstOrDefault(e => e.LocalName == "custGeom");
			if (customGeom is not null)
			{
				elements.Add(new DrawingCustomGeometryRunElement
				{
					WidthEmu = extent?.Cx ?? 0,
					HeightEmu = extent?.Cy ?? 0,
					Commands = CustomGeometryParser.Parse(customGeom),
					Fill = ShapeFillParser.Parse(inlineShapeProperties),
					Outline = ShapeOutlineParser.Parse(inlineShapeProperties),
					TextFrame = inlineTextFrame,
					Transform = inlineTransform,
					AnchorPlacement = AnchorPlacementInfo.None
				});
				return;
			}

			// Check for chart reference (c:chart element).
			var inlineChart = inline.Descendants().FirstOrDefault(e => e.LocalName == "chart");
			if (inlineChart is not null)
			{
				var chartRelId = inlineChart.GetAttributes().FirstOrDefault(a => a.LocalName == "id").Value ?? string.Empty;
				elements.Add(new ChartRunElement
				{
					RelationshipId = chartRelId,
					WidthEmu = extent?.Cx ?? 0,
					HeightEmu = extent?.Cy ?? 0
				});
				return;
			}

			// Check for SmartArt (dgm:relIds element).
			var inlineRelIds = inline.Descendants().FirstOrDefault(e => e.LocalName == "relIds");
			if (inlineRelIds is not null)
			{
				var smartArtRelId = inlineRelIds.GetAttributes().FirstOrDefault(a => a.LocalName == "dm").Value ?? string.Empty;
				var hasFallback = inline.Descendants<A.ShapeProperties>().Any();
				elements.Add(new SmartArtRunElement
				{
					RelationshipId = smartArtRelId,
					WidthEmu = extent?.Cx ?? 0,
					HeightEmu = extent?.Cy ?? 0,
					HasFallback = hasFallback
				});
				return;
			}

			var blip = inline.Descendants<A.Blip>().FirstOrDefault();
			var sourceRectangle = inline.Descendants<A.SourceRectangle>().FirstOrDefault();

			elements.Add(new InlineImageRunElement
			{
				RelationshipId = blip?.Embed?.Value ?? string.Empty,
				WidthEmu = extent?.Cx ?? 0,
				HeightEmu = extent?.Cy ?? 0,
				CropLeft = ParsePercentage(sourceRectangle?.Left),
				CropTop = ParsePercentage(sourceRectangle?.Top),
				CropRight = ParsePercentage(sourceRectangle?.Right),
				CropBottom = ParsePercentage(sourceRectangle?.Bottom)
			});
			return;
		}

		var anchor = drawing.GetFirstChild<DW.Anchor>();
		if (anchor is null)
		{
			return;
		}

		var anchorExtent = anchor.Extent;
		var anchorShapeProperties = anchor.Descendants<A.ShapeProperties>().FirstOrDefault();
		var anchorTextFrame = ShapeTextFrameParser.Parse(anchor);
		var anchorTransform = ShapeTransformParser.Parse(anchorShapeProperties);
		var anchorPlacement = ParseAnchorPlacement(anchor);

		// Check for grouped shapes (wpg:wgp) first.
		var anchorWgp = anchor.Descendants().FirstOrDefault(e => e.LocalName == "wgp");
		if (anchorWgp is not null)
		{
			elements.Add(GroupShapeParser.Parse(anchorWgp, anchorExtent?.Cx ?? 0, anchorExtent?.Cy ?? 0));
			return;
		}

		// Check for DrawingML shape in anchor before image blip.
		var anchorPresetGeom = anchor.Descendants<A.PresetGeometry>().FirstOrDefault();
		if (anchorPresetGeom is not null)
		{
			elements.Add(ParseDrawingShape(anchorExtent?.Cx ?? 0, anchorExtent?.Cy ?? 0, anchorPresetGeom, anchorShapeProperties, anchorTextFrame, anchorTransform, anchorPlacement));
			return;
		}

		var anchorCustomGeom = anchor.Descendants().FirstOrDefault(e => e.LocalName == "custGeom");
		if (anchorCustomGeom is not null)
		{
			elements.Add(new DrawingCustomGeometryRunElement
			{
				WidthEmu = anchorExtent?.Cx ?? 0,
				HeightEmu = anchorExtent?.Cy ?? 0,
				Commands = CustomGeometryParser.Parse(anchorCustomGeom),
				Fill = ShapeFillParser.Parse(anchorShapeProperties),
				Outline = ShapeOutlineParser.Parse(anchorShapeProperties),
				TextFrame = anchorTextFrame,
				Transform = anchorTransform,
				AnchorPlacement = anchorPlacement
			});
			return;
		}

		// Check for chart reference (c:chart element).
		var anchorChart = anchor.Descendants().FirstOrDefault(e => e.LocalName == "chart");
		if (anchorChart is not null)
		{
			var anchorChartRelId = anchorChart.GetAttributes().FirstOrDefault(a => a.LocalName == "id").Value ?? string.Empty;
			elements.Add(new ChartRunElement
			{
				RelationshipId = anchorChartRelId,
				WidthEmu = anchorExtent?.Cx ?? 0,
				HeightEmu = anchorExtent?.Cy ?? 0
			});
			return;
		}

		// Check for SmartArt (dgm:relIds element).
		var anchorRelIds = anchor.Descendants().FirstOrDefault(e => e.LocalName == "relIds");
		if (anchorRelIds is not null)
		{
			var anchorSmartArtRelId = anchorRelIds.GetAttributes().FirstOrDefault(a => a.LocalName == "dm").Value ?? string.Empty;
			var anchorHasFallback = anchor.Descendants<A.ShapeProperties>().Any();
			elements.Add(new SmartArtRunElement
			{
				RelationshipId = anchorSmartArtRelId,
				WidthEmu = anchorExtent?.Cx ?? 0,
				HeightEmu = anchorExtent?.Cy ?? 0,
				HasFallback = anchorHasFallback
			});
			return;
		}

		var anchorBlip = anchor.Descendants<A.Blip>().FirstOrDefault();
		var anchorSourceRectangle = anchor.Descendants<A.SourceRectangle>().FirstOrDefault();
		var horizontalPosition = anchor.GetFirstChild<DW.HorizontalPosition>();
		var verticalPosition = anchor.GetFirstChild<DW.VerticalPosition>();

		elements.Add(new AnchorImageRunElement
		{
			RelationshipId = anchorBlip?.Embed?.Value ?? string.Empty,
			WidthEmu = anchorExtent?.Cx ?? 0,
			HeightEmu = anchorExtent?.Cy ?? 0,
			CropLeft = ParsePercentage(anchorSourceRectangle?.Left),
			CropTop = ParsePercentage(anchorSourceRectangle?.Top),
			CropRight = ParsePercentage(anchorSourceRectangle?.Right),
			CropBottom = ParsePercentage(anchorSourceRectangle?.Bottom),
			HorizontalRelativeFrom = ParseHorizontalRelativeFrom(horizontalPosition?.RelativeFrom?.Value),
			VerticalRelativeFrom = ParseVerticalRelativeFrom(verticalPosition?.RelativeFrom?.Value),
			HorizontalOffsetEmu = ParseOffset(horizontalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			VerticalOffsetEmu = ParseOffset(verticalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			HorizontalAlignment = ParseHorizontalAlignment(horizontalPosition?.GetFirstChild<DW.HorizontalAlignment>()?.InnerText),
			VerticalAlignment = ParseVerticalAlignment(verticalPosition?.GetFirstChild<DW.VerticalAlignment>()?.InnerText),
			BehindDocument = ParseOnOffValue(anchor.BehindDoc)
		});
	}

	private static DrawingShapeRunElement ParseDrawingShape(long widthEmu, long heightEmu, A.PresetGeometry presetGeom, A.ShapeProperties? shapeProperties, ShapeTextFrameInfo textFrame, ShapeTransformInfo transform, AnchorPlacementInfo? anchorPlacement = null)
	{
		var rawName = presetGeom.Preset?.InnerText ?? string.Empty;
		return new DrawingShapeRunElement
		{
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			PresetKind = PresetGeometryParser.Parse(rawName),
			RawPresetName = rawName,
			Fill = ShapeFillParser.Parse(shapeProperties),
			Outline = ShapeOutlineParser.Parse(shapeProperties),
			TextFrame = textFrame,
			Transform = transform,
			AnchorPlacement = anchorPlacement ?? AnchorPlacementInfo.None
		};
	}

	private static AnchorPlacementInfo ParseAnchorPlacement(DW.Anchor anchor)
	{
		ArgumentNullException.ThrowIfNull(anchor);

		var horizontalPosition = anchor.GetFirstChild<DW.HorizontalPosition>();
		var verticalPosition = anchor.GetFirstChild<DW.VerticalPosition>();

		return new AnchorPlacementInfo
		{
			HorizontalRelativeFrom = ParseHorizontalRelativeFrom(horizontalPosition?.RelativeFrom?.Value),
			VerticalRelativeFrom = ParseVerticalRelativeFrom(verticalPosition?.RelativeFrom?.Value),
			HorizontalOffsetEmu = ParseOffset(horizontalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			VerticalOffsetEmu = ParseOffset(verticalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			HorizontalAlignment = ParseHorizontalAlignment(horizontalPosition?.GetFirstChild<DW.HorizontalAlignment>()?.InnerText),
			VerticalAlignment = ParseVerticalAlignment(verticalPosition?.GetFirstChild<DW.VerticalAlignment>()?.InnerText),
			BehindDocument = ParseOnOffValue(anchor.BehindDoc),
			WrapStyle = ParseWrapStyle(anchor),
			DistanceTopEmu = anchor.DistanceFromTop?.Value ?? 0U,
			DistanceBottomEmu = anchor.DistanceFromBottom?.Value ?? 0U,
			DistanceLeftEmu = anchor.DistanceFromLeft?.Value ?? 0U,
			DistanceRightEmu = anchor.DistanceFromRight?.Value ?? 0U
		};
	}

	private static AnchorWrapStyle ParseWrapStyle(DW.Anchor anchor)
	{
		foreach (var child in anchor.ChildElements)
		{
			switch (child.LocalName)
			{
				case "wrapSquare":
					return AnchorWrapStyle.Square;
				case "wrapTight":
				case "wrapThrough":
					return AnchorWrapStyle.Tight;
				case "wrapTopAndBottom":
				case "wrapTopBottom":
					return AnchorWrapStyle.TopAndBottom;
				case "wrapNone":
					return AnchorWrapStyle.None;
			}
		}

		return AnchorWrapStyle.None;
	}

	private static int ParsePercentage(Int32Value? value)
	{
		if (value is null)
		{
			return 0;
		}

		return value.Value;
	}

	private static long ParseOffset(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0;
		}

		return long.TryParse(value, out var parsed) ? parsed : 0;
	}

	private static AnchorRelativeFrom ParseHorizontalRelativeFrom(DW.HorizontalRelativePositionValues? value)
	{
		if (value is null)
		{
			return AnchorRelativeFrom.Unknown;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Page)
		{
			return AnchorRelativeFrom.Page;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Margin)
		{
			return AnchorRelativeFrom.Margin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Column)
		{
			return AnchorRelativeFrom.Column;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.Character)
		{
			return AnchorRelativeFrom.Character;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.LeftMargin)
		{
			return AnchorRelativeFrom.LeftMargin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.RightMargin)
		{
			return AnchorRelativeFrom.RightMargin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.InsideMargin)
		{
			return AnchorRelativeFrom.InsideMargin;
		}

		if (value.Value == DW.HorizontalRelativePositionValues.OutsideMargin)
		{
			return AnchorRelativeFrom.OutsideMargin;
		}

		return AnchorRelativeFrom.Unknown;
	}

	private static AnchorRelativeFrom ParseVerticalRelativeFrom(DW.VerticalRelativePositionValues? value)
	{
		if (value is null)
		{
			return AnchorRelativeFrom.Unknown;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Page)
		{
			return AnchorRelativeFrom.Page;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Margin)
		{
			return AnchorRelativeFrom.Margin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Paragraph)
		{
			return AnchorRelativeFrom.Paragraph;
		}

		if (value.Value == DW.VerticalRelativePositionValues.Line)
		{
			return AnchorRelativeFrom.Line;
		}

		if (value.Value == DW.VerticalRelativePositionValues.TopMargin)
		{
			return AnchorRelativeFrom.TopMargin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.BottomMargin)
		{
			return AnchorRelativeFrom.BottomMargin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.InsideMargin)
		{
			return AnchorRelativeFrom.InsideMargin;
		}

		if (value.Value == DW.VerticalRelativePositionValues.OutsideMargin)
		{
			return AnchorRelativeFrom.OutsideMargin;
		}

		return AnchorRelativeFrom.Unknown;
	}

	private static AnchorAlignment ParseHorizontalAlignment(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return AnchorAlignment.None;
		}

		return value.Trim().ToLowerInvariant() switch
		{
			"left" => AnchorAlignment.Left,
			"center" => AnchorAlignment.Center,
			"right" => AnchorAlignment.Right,
			"inside" => AnchorAlignment.Inside,
			"outside" => AnchorAlignment.Outside,
			_ => AnchorAlignment.None
		};
	}

	private static AnchorAlignment ParseVerticalAlignment(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return AnchorAlignment.None;
		}

		return value.Trim().ToLowerInvariant() switch
		{
			"top" => AnchorAlignment.Top,
			"center" => AnchorAlignment.Center,
			"bottom" => AnchorAlignment.Bottom,
			"inside" => AnchorAlignment.Inside,
			"outside" => AnchorAlignment.Outside,
			_ => AnchorAlignment.None
		};
	}

	private static bool ParseOnOffValue(BooleanValue? value)
	{
		if (value is null)
		{
			return false;
		}

		return value.Value;
	}
}
