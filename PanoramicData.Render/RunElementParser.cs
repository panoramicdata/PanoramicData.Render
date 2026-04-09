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

	private static void ParseDrawing(Drawing drawing, List<RunElement> elements)
	{
		var inline = drawing.GetFirstChild<DW.Inline>();
		if (inline is not null)
		{
			var extent = inline.Extent;
			var inlineShapeProperties = inline.Descendants<A.ShapeProperties>().FirstOrDefault();

			// Check for DrawingML shape (a:prstGeom) before image blip.
			var presetGeom = inline.Descendants<A.PresetGeometry>().FirstOrDefault();
			if (presetGeom is not null)
			{
				elements.Add(ParseDrawingShape(extent?.Cx ?? 0, extent?.Cy ?? 0, presetGeom, inlineShapeProperties));
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
					Fill = ShapeFillParser.Parse(inlineShapeProperties)
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

		// Check for DrawingML shape in anchor before image blip.
		var anchorPresetGeom = anchor.Descendants<A.PresetGeometry>().FirstOrDefault();
		if (anchorPresetGeom is not null)
		{
			elements.Add(ParseDrawingShape(anchorExtent?.Cx ?? 0, anchorExtent?.Cy ?? 0, anchorPresetGeom, anchorShapeProperties));
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
				Fill = ShapeFillParser.Parse(anchorShapeProperties)
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

	private static DrawingShapeRunElement ParseDrawingShape(long widthEmu, long heightEmu, A.PresetGeometry presetGeom, A.ShapeProperties? shapeProperties)
	{
		var rawName = presetGeom.Preset?.InnerText ?? string.Empty;
		return new DrawingShapeRunElement
		{
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			PresetKind = PresetGeometryParser.Parse(rawName),
			RawPresetName = rawName,
			Fill = ShapeFillParser.Parse(shapeProperties)
		};
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
