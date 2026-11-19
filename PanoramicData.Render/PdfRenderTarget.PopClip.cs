namespace PanoramicData.Render;

using SkiaSharp;

internal sealed partial class PdfRenderTarget : IRenderTarget, IDisposable
{
	/// <inheritdoc/>
	public void PopClip()
	{
		if (_clipStack.Count == 0)
		{
			return;
		}

		_clipStack.Pop();
		GetCanvas().Restore();
	}

	/// <inheritdoc/>
	public void SetHyperlink(RenderRect rect, string uri)
	{
		ArgumentNullException.ThrowIfNull(uri);

		var skRect = CreateSkRect(rect);
		if (uri.StartsWith('#'))
		{
			// Internal bookmark link — target a named destination within the document
			GetCanvas().DrawLinkDestinationAnnotation(skRect, uri[1..]);
		}
		else
		{
			// External URL link
			GetCanvas().DrawUrlAnnotation(skRect, uri);
		}
	}

	/// <inheritdoc/>
	public void SetNamedDestination(string name, float xTwips, float yTwips)
	{
		ArgumentNullException.ThrowIfNull(name);

		GetCanvas().DrawNamedDestinationAnnotation(
			new SKPoint(TwipsToPoints(xTwips), TwipsToPoints(yTwips)), name);
	}

	/// <inheritdoc/>
	public void DrawRotatedText(string text, float centerXTwips, float centerYTwips, float rotationDegrees, RenderFont font, RenderBrush brush)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(font.Family);
		ArgumentNullException.ThrowIfNull(brush);

		var canvas = GetCanvas();
		canvas.Save();
		canvas.Translate(TwipsToPoints(centerXTwips), TwipsToPoints(centerYTwips));
		canvas.RotateDegrees(rotationDegrees);
		DrawStyledText(canvas, text, 0f, 0f, SKTextAlign.Center, font, brush);
		canvas.Restore();
	}

	/// <inheritdoc/>
	public void DrawRotatedImage(ImageData image, float centerXTwips, float centerYTwips, float widthTwips, float heightTwips, float rotationDegrees, float opacity)
	{
		ArgumentNullException.ThrowIfNull(image);
		var canvas = GetCanvas();

		using var bitmap = SKBitmap.Decode(image.Data);
		if (bitmap is null)
		{
			return;
		}

		using var paint = new SKPaint();
		paint.Color = paint.Color.WithAlpha((byte)Math.Clamp(opacity * 255f, 0, 255));

		var halfW = TwipsToPoints(widthTwips / 2f);
		var halfH = TwipsToPoints(heightTwips / 2f);
		var destRect = new SKRect(-halfW, -halfH, halfW, halfH);

		canvas.Save();
		canvas.Translate(TwipsToPoints(centerXTwips), TwipsToPoints(centerYTwips));
		if (Math.Abs(rotationDegrees) > 0.001f)
		{
			canvas.RotateDegrees(rotationDegrees);
		}

		canvas.DrawBitmap(bitmap, destRect, SKSamplingOptions.Default, paint);
		canvas.Restore();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		if (!_isFinalized)
		{
			EndCurrentPage();
			_document.Close();
			_isFinalized = true;
		}

		_document.Dispose();
		_stream.Dispose();
		_isDisposed = true;
	}

	private static float TwipsToPoints(float twips)
	{
		return TwipConverter.TwipsToPoints(twips);
	}

	private static SKRect CreateSkRect(RenderRect rect)
	{
		return new SKRect(
			TwipsToPoints(rect.XTwips),
			TwipsToPoints(rect.YTwips),
			TwipsToPoints(rect.XTwips + rect.WidthTwips),
			TwipsToPoints(rect.YTwips + rect.HeightTwips));
	}

	private static void DrawStyledText(SKCanvas canvas, string text, float x, float y, SKTextAlign align, RenderFont font, RenderBrush brush)
	{
		using var paint = CreatePaintFromBrush(brush);
		paint.IsAntialias = true;
		using var typeface = SKTypeface.FromFamilyName(font.Family, SkiaFontStyles.FromRenderFont(font));
		using var skFont = new SKFont(typeface, TwipsToPoints(TwipConverter.PointsToTwips(font.SizePoints)));
		canvas.DrawText(text, x, y, align, skFont, paint);
	}

	private static SKPaint CreatePaintFromBrush(RenderBrush brush)
	{
		var paint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Fill,
			Color = brush switch
			{
				SolidRenderBrush solid => ToSkColor(solid.Color),
				LinearGradientRenderBrush gradient => ToSkColor(gradient.StartColor),
				_ => SKColors.Black
			}
		};

		return paint;
	}

	private static SKPaint CreateStrokePaint(RenderStroke stroke)
	{
		return new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			Color = ToSkColor(stroke.Color),
			StrokeWidth = MathF.Max(TwipsToPoints(stroke.WidthTwips), 0.25f)
		};
	}

	private static SKColor ToSkColor(RenderColor color)
	{
		return new SKColor(color.R, color.G, color.B, color.A);
	}


	private static void ApplyTwipToPointScale(SKPath path)
	{
		var matrix = SKMatrix.CreateScale(1f / TwipConverter.TwipsPerPoint, 1f / TwipConverter.TwipsPerPoint);
		path.Transform(matrix);
	}

	private static SKDocumentPdfMetadata CreateSkMetadata(PdfMetadata metadata)
	{
		return new SKDocumentPdfMetadata
		{
			Title = metadata.Title,
			Author = metadata.Author,
			Creator = "PanoramicData.Render",
			Producer = "SkiaSharp",
			Creation = metadata.CreationDateUtc,
			Modified = metadata.CreationDateUtc
		};
	}
}
