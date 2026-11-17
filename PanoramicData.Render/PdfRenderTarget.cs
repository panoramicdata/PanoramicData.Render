namespace PanoramicData.Render;

using SkiaSharp;

/// <summary>
/// PDF render target that converts drawing commands to SkiaSharp PDF canvas operations.
/// </summary>
internal sealed partial class PdfRenderTarget : IRenderTarget, IDisposable
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

		SKBitmap? bitmap;
		try
		{
			bitmap = SKBitmap.Decode(image.Data);
		}
		catch
		{
			// Corrupt image data — skip gracefully
			return;
		}

		if (bitmap is null)
		{
			return;
		}

		using (bitmap)
		{
			canvas.DrawBitmap(bitmap, CreateSkRect(rect), SKSamplingOptions.Default);
		}
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
}
