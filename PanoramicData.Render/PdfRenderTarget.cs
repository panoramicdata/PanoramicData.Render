namespace PanoramicData.Render;

using SkiaSharp;

/// <summary>
/// PDF render target that converts drawing commands to SkiaSharp PDF canvas operations.
/// </summary>
internal sealed class PdfRenderTarget : IRenderTarget, IDisposable
{
	private readonly MemoryStream _stream;
	private readonly SKDocument _document;
	private readonly SKCanvas _canvas;
	private readonly Stack<RenderRect> _clipStack = new();
	private readonly float _pageWidthTwips;
	private readonly float _pageHeightTwips;
	private bool _isDisposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="PdfRenderTarget"/> class.
	/// </summary>
	/// <param name="pageWidthTwips">The page width in twips.</param>
	/// <param name="pageHeightTwips">The page height in twips.</param>
	public PdfRenderTarget(float pageWidthTwips, float pageHeightTwips)
	{
		if (pageWidthTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageWidthTwips));
		}

		if (pageHeightTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageHeightTwips));
		}

		_pageWidthTwips = pageWidthTwips;
		_pageHeightTwips = pageHeightTwips;
		_stream = new MemoryStream();
		_document = SKDocument.CreatePdf(_stream);
		_canvas = _document.BeginPage(TwipsToPoints(pageWidthTwips), TwipsToPoints(pageHeightTwips));
	}

	/// <summary>
	/// Completes PDF generation and returns the raw PDF bytes.
	/// </summary>
	/// <returns>The generated PDF document.</returns>
	public byte[] BuildPdf()
	{
		ThrowIfDisposed();

		_document.EndPage();
		_document.Close();
		return _stream.ToArray();
	}

	/// <inheritdoc/>
	public void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(font.Family);
		ArgumentNullException.ThrowIfNull(brush);

		using var paint = CreatePaintFromBrush(brush);
		paint.IsAntialias = true;
		using var typeface = SKTypeface.FromFamilyName(font.Family, FontStyleFromRenderFont(font));
		using var skFont = new SKFont(typeface, TwipsToPoints(TwipConverter.PointsToTwips(font.SizePoints)));
		_canvas.DrawText(text, TwipsToPoints(baselineXTwips), TwipsToPoints(baselineYTwips), SKTextAlign.Left, skFont, paint);
	}

	/// <inheritdoc/>
	public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
	{
		using var paint = CreateStrokePaint(stroke);
		_canvas.DrawLine(
			TwipsToPoints(from.XTwips),
			TwipsToPoints(from.YTwips),
			TwipsToPoints(to.XTwips),
			TwipsToPoints(to.YTwips),
			paint);
	}

	/// <inheritdoc/>
	public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
	{
		var skRect = CreateSkRect(rect);
		if (fill is not null)
		{
			using var fillPaint = CreatePaintFromBrush(fill);
			fillPaint.Style = SKPaintStyle.Fill;
			_canvas.DrawRect(skRect, fillPaint);
		}

		if (stroke is not null)
		{
			using var strokePaint = CreateStrokePaint(stroke.Value);
			_canvas.DrawRect(skRect, strokePaint);
		}
	}

	/// <inheritdoc/>
	public void DrawImage(ImageData image, RenderRect rect)
	{
		ArgumentNullException.ThrowIfNull(image);

		using var bitmap = SKBitmap.Decode(image.Data);
		if (bitmap is null)
		{
			return;
		}

		_canvas.DrawBitmap(bitmap, CreateSkRect(rect));
	}

	/// <inheritdoc/>
	public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
	{
		ArgumentNullException.ThrowIfNull(pathData);

		using var path = SKPath.ParseSvgPathData(pathData);
		if (path is null)
		{
			return;
		}

		ApplyTwipToPointScale(path);

		if (fill is not null)
		{
			using var fillPaint = CreatePaintFromBrush(fill);
			fillPaint.Style = SKPaintStyle.Fill;
			_canvas.DrawPath(path, fillPaint);
		}

		if (stroke is not null)
		{
			using var strokePaint = CreateStrokePaint(stroke.Value);
			_canvas.DrawPath(path, strokePaint);
		}
	}

	/// <inheritdoc/>
	public void PushClip(RenderRect clipRect)
	{
		_clipStack.Push(clipRect);
		_canvas.Save();
		_canvas.ClipRect(CreateSkRect(clipRect));
	}

	/// <inheritdoc/>
	public void PopClip()
	{
		if (_clipStack.Count == 0)
		{
			return;
		}

		_clipStack.Pop();
		_canvas.Restore();
	}

	/// <inheritdoc/>
	public void SetHyperlink(RenderRect rect, string uri)
	{
		ArgumentNullException.ThrowIfNull(uri);
		_ = rect;

		// SkiaSharp's managed PDF APIs used in this phase do not expose link annotation authoring.
		// Hyperlink support is tracked for a later phase once a reliable PDF annotation path is chosen.
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
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

	private static SKFontStyle FontStyleFromRenderFont(RenderFont font)
	{
		return new SKFontStyle(
			font.IsBold ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal,
			(int)SKFontStyleWidth.Normal,
			font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
	}

	private static void ApplyTwipToPointScale(SKPath path)
	{
		var matrix = SKMatrix.CreateScale(1f / TwipConverter.TwipsPerPoint, 1f / TwipConverter.TwipsPerPoint);
		path.Transform(matrix);
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
}
