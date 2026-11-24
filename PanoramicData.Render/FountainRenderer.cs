using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>Renders plain-text Fountain scripts as paginated PDFs.</summary>
/// <param name="options">Font resolution and optional output page range.</param>
public sealed class FountainRenderer(RenderOptions? options = null)
{
	private static readonly string[] _scriptFontFamilies = ["Courier New", "Liberation Mono", "DejaVu Sans Mono", "Courier", "monospace"];
	private static readonly SolidRenderBrush _ink = new(new RenderColor(0, 0, 0));

	/// <summary>Renders a script using the requested layout without changing the source.</summary>
	/// <param name="source">The decoded Fountain source.</param>
	/// <param name="profile">The layout and pagination conventions to apply.</param>
	/// <param name="cancellationToken">A token used to cancel parsing, layout or painting.</param>
	/// <returns>The PDF document bytes.</returns>
	public byte[] RenderPdf(string source, FountainRenderProfile profile, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		cancellationToken.ThrowIfCancellationRequested();
		var format = FountainPdfLayout.For(profile);
		var script = FountainScriptParser.Parse(source, cancellationToken);
		var renderOptions = options ?? new RenderOptions();
		var resolver = new FontResolver(renderOptions);
		var typeface = ResolveFont(resolver);
		using var font = new SKFont(typeface, 12);
		var lines = new FountainLineFormatter(font);
		var pages = PageRangeSelector.Apply(new FountainPageBuilder(format, lines, cancellationToken).Build(script), renderOptions.PageRange);
		if (pages.Count == 0)
		{
			return [];
		}

		var width = TwipConverter.PointsToTwips(format.PageWidth);
		var height = TwipConverter.PointsToTwips(format.PageHeight);
		var metadata = new PdfMetadata(script.TitlePage.GetValueOrDefault("Title"),
			script.TitlePage.GetValueOrDefault("Author") ?? script.TitlePage.GetValueOrDefault("Authors"), null);
		using var target = new PdfRenderTarget(width, height, metadata);
		for (var index = 0; index < pages.Count; index++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (index > 0)
			{
				target.BeginPage(width, height);
			}

			foreach (var line in pages[index].Lines)
			{
				PaintLine(target, font, lines, line);
			}
		}

		return target.BuildPdf();
	}

	private static void PaintLine(PdfRenderTarget target, SKFont font, FountainLineFormatter lines, FountainPdfLine line)
	{
		var x = TwipConverter.PointsToTwips(line.X);
		var y = TwipConverter.PointsToTwips(line.Baseline);
		target.DrawMeasuredText(line.Text, x, y, font, _ink);
		if (line.Underline)
		{
			var underlineY = y + TwipConverter.PointsToTwips(1.5f);
			var end = x + TwipConverter.PointsToTwips(lines.Measure(line.Text));
			target.DrawLine(new RenderPoint(x, underlineY), new RenderPoint(end, underlineY), new RenderStroke(_ink.Color, 12));
		}
	}

	private static SKTypeface ResolveFont(FontResolver resolver)
	{
		foreach (var family in _scriptFontFamilies)
		{
			if (resolver.TryGetTypeface(family, bold: false, italic: false, out var typeface) && typeface is { IsFixedPitch: true })
			{
				return typeface;
			}
		}

		foreach (var family in _scriptFontFamilies)
		{
			var typeface = SKFontManager.Default.MatchFamily(family);
			if (typeface is { IsFixedPitch: true })
			{
				return typeface;
			}
			typeface?.Dispose();
		}

		return SKTypeface.Default;
	}
}
