namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

internal sealed class StyleDocument : IDisposable
{
	private readonly MemoryStream _stream;
	private readonly DocxDocument _document;

	private StyleDocument(MemoryStream stream, DocxDocument document)
	{
		_stream = stream;
		_document = document;
	}

	public Styles? Styles => _document.StylesPart?.Styles;

	public static StyleDocument Create(Styles styles)
	{
		var stream = TestDocxBuilder.CreateDocxWithStyles(styles);
		return new StyleDocument(stream, DocxDocument.Load(stream));
	}

	public void Dispose()
	{
		_document.Dispose();
		_stream.Dispose();
	}
}