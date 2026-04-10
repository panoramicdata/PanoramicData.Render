namespace PanoramicData.Render;

using SkiaSharp;

/// <summary>
/// PDF render target that converts drawing commands to SkiaSharp PDF canvas operations.
/// </summary>
internal sealed class PdfRenderTarget : IRenderTarget, IDisposable
{
	private readonly MemoryStream _stream;
	private readonly SKDocument _document;
	private SKCanvas? _canvas;
	private readonly Stack<RenderRect> _clipStack = new();
	private bool _isDisposed;
	private bool _isFinalized;
	private bool _hasOpenPage;

	/// <summary>
	/// Initializes a new instance of the <see cref="PdfRenderTarget"/> class.
	/// </summary>
	/// <param name="pageWidthTwips">The page width in twips.</param>
	/// <param name="pageHeightTwips">The page height in twips.</param>
	/// <param name="metadata">Optional PDF metadata to embed in document properties.</param>
	public PdfRenderTarget(float pageWidthTwips, float pageHeightTwips, PdfMetadata? metadata = null)
	{
		if (pageWidthTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageWidthTwips));
		}

		if (pageHeightTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageHeightTwips));
		}

		_stream = new MemoryStream();
		_document = metadata is null
			? SKDocument.CreatePdf(_stream)
			: SKDocument.CreatePdf(_stream, CreateSkMetadata(metadata.Value));
		BeginPage(pageWidthTwips, pageHeightTwips);
	}

	/// <summary>
	/// Starts a new PDF page, ending the current page if one is open.
	/// </summary>
	/// <param name="pageWidthTwips">The new page width in twips.</param>
	/// <param name="pageHeightTwips">The new page height in twips.</param>
	public void BeginPage(float pageWidthTwips, float pageHeightTwips)
	{
		ThrowIfDisposed();

		if (_isFinalized)
		{
			throw new InvalidOperationException("Cannot begin a page after BuildPdf has finalized the document.");
		}

		if (pageWidthTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageWidthTwips));
		}

		if (pageHeightTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageHeightTwips));
		}

		EndCurrentPage();
		_canvas = _document.BeginPage(TwipsToPoints(pageWidthTwips), TwipsToPoints(pageHeightTwips));
		_hasOpenPage = true;
	}

	/// <summary>
	/// Completes PDF generation and returns the raw PDF bytes.
	/// </summary>
	/// <returns>The generated PDF document.</returns>
	public byte[] BuildPdf()
	{
		ThrowIfDisposed();
		if (_isFinalized)
		{
			return _stream.ToArray();
		}

		EndCurrentPage();
		_document.Close();
		_isFinalized = true;
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
		GetCanvas().DrawText(text, TwipsToPoints(baselineXTwips), TwipsToPoints(baselineYTwips), SKTextAlign.Left, skFont, paint);
	}

	/// <inheritdoc/>
	public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
	{
		using var paint = CreateStrokePaint(stroke);
		GetCanvas().DrawLine(
			TwipsToPoints(from.XTwips),
			TwipsToPoints(from.YTwips),
			TwipsToPoints(to.XTwips),
			TwipsToPoints(to.YTwips),
			paint);
	}

	/// <inheritdoc/>
	public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
	{
		var canvas = GetCanvas();
		var skRect = CreateSkRect(rect);
		if (fill is not null)
		{
			using var fillPaint = CreatePaintFromBrush(fill);
			fillPaint.Style = SKPaintStyle.Fill;
			canvas.DrawRect(skRect, fillPaint);
		}

		if (stroke is not null)
		{
			using var strokePaint = CreateStrokePaint(stroke.Value);
			canvas.DrawRect(skRect, strokePaint);
		}
	}

	/// <inheritdoc/>
	public void DrawImage(ImageData image, RenderRect rect)
	{
		ArgumentNullException.ThrowIfNull(image);
		var canvas = GetCanvas();

		using var bitmap = SKBitmap.Decode(image.Data);
		if (bitmap is null)
		{
			return;
		}

		canvas.DrawBitmap(bitmap, CreateSkRect(rect));
	}

	/// <inheritdoc/>
	public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
	{
		ArgumentNullException.ThrowIfNull(pathData);
		var canvas = GetCanvas();

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
			canvas.DrawPath(path, fillPaint);
		}

		if (stroke is not null)
		{
			using var strokePaint = CreateStrokePaint(stroke.Value);
			canvas.DrawPath(path, strokePaint);
		}
	}

	/// <inheritdoc/>
	public void PushClip(RenderRect clipRect)
	{
		_clipStack.Push(clipRect);
		var canvas = GetCanvas();
		canvas.Save();
		canvas.ClipRect(CreateSkRect(clipRect));
	}

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

	private void EndCurrentPage()
	{
		if (!_hasOpenPage)
		{
			return;
		}

		while (_clipStack.Count > 0)
		{
			GetCanvas().Restore();
			_clipStack.Pop();
		}

		_document.EndPage();
		_canvas = null;
		_hasOpenPage = false;
	}

	private SKCanvas GetCanvas()
	{
		if (_canvas is null)
		{
			throw new InvalidOperationException("No active PDF page is open.");
		}

		return _canvas;
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
}
